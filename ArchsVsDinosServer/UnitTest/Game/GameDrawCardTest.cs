using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Board;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using ArchsVsDinosServer.Interfaces.Game;
using ArchsVsDinosServer.Utils;
using Contracts;
using Contracts.DTO.Game_DTO;
using Contracts.DTO.Game_DTO.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.Game
{
    [TestClass]
    public class GameLogicDrawCardTest : BaseTestClass
    {
        private Mock<GameSessionManager> mockSessionManager;
        private Mock<GameSetupHandler> mockSetupHandler;
        private Mock<IGameNotifier> mockGameNotifier;
        private Mock<IStatisticsManager> mockStatisticsManager;
        private GameCoreContext gameCoreContext;
        private GameLogic gameLogic;
        private GameSession testSession;
        private PlayerSession testPlayer;

        [TestInitialize]
        public void SetupDrawCardTests()
        {
            BaseSetup();

            mockSessionManager = new Mock<GameSessionManager>(mockLoggerHelper.Object);
            mockSetupHandler = new Mock<GameSetupHandler>();
            mockGameNotifier = new Mock<IGameNotifier>();
            mockStatisticsManager = new Mock<IStatisticsManager>();

            gameCoreContext = new GameCoreContext(
                mockSessionManager.Object,
                mockSetupHandler.Object
            );

            var dependencies = new GameLogicDependencies(
                gameCoreContext,
                mockLoggerHelper.Object,
                mockGameNotifier.Object,
                mockStatisticsManager.Object
            );

            gameLogic = new GameLogic(dependencies);

            var mockCentralBoard = new CentralBoard();
            testSession = new GameSession("TEST-MATCH", mockCentralBoard, mockLoggerHelper.Object);
            testSession.MarkAsStarted();

            testPlayer = new PlayerSession(1, "TestPlayer", null);
            testSession.AddPlayer(testPlayer);
            testSession.StartTurn(1);

            mockSessionManager.Setup(x => x.GetSession("TEST-MATCH")).Returns(testSession);
        }

        [TestMethod]
        public void TestDrawCardReturnsCard()
        {
            // Arrange
            var deck = new List<int> { 28 }; // Head card Sand
            testSession.SetDrawDeck(deck);

            // Act
            var result = gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestDrawCardReturnsCorrectCardId()
        {
            // Arrange
            var deck = new List<int> { 28 };
            testSession.SetDrawDeck(deck);

            // Act
            var result = gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert
            Assert.AreEqual(28, result.IdCard);
        }

        [TestMethod]
        public void TestDrawCardRemovesCardFromDeck()
        {
            // Arrange
            var deck = new List<int> { 28, 29, 30 };
            testSession.SetDrawDeck(deck);

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert
            Assert.AreEqual(2, testSession.DrawDeck.Count);
        }

        [TestMethod]
        public void TestDrawCardAddsCardToPlayerHand()
        {
            // Arrange
            var deck = new List<int> { 28 };
            testSession.SetDrawDeck(deck);

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert
            Assert.AreEqual(1, testPlayer.Hand.Count);
        }

        [TestMethod]
        public void TestDrawCardConsumesMoves()
        {
            // Arrange
            var deck = new List<int> { 28 };
            testSession.SetDrawDeck(deck);
            int initialMoves = testSession.RemainingMoves;

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert
            Assert.AreEqual(initialMoves - 1, testSession.RemainingMoves);
        }

        [TestMethod]
        public void TestDrawCardNotifiesCardDrawn()
        {
            // Arrange
            var deck = new List<int> { 28 };
            testSession.SetDrawDeck(deck);

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyCardDrawn(It.IsAny<CardDrawnDTO>()), Times.Once);
        }

        [TestMethod]
        public void TestDrawCardNotificationContainsCorrectUserId()
        {
            // Arrange
            var deck = new List<int> { 28 };
            testSession.SetDrawDeck(deck);

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyCardDrawn(
                It.Is<CardDrawnDTO>(dto => dto.PlayerUserId == 1)),
                Times.Once);
        }

        [TestMethod]
        public void TestDrawCardNotificationContainsCorrectMatchCode()
        {
            // Arrange
            var deck = new List<int> { 28 };
            testSession.SetDrawDeck(deck);

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyCardDrawn(
                It.Is<CardDrawnDTO>(dto => dto.MatchCode == "TEST-MATCH")),
                Times.Once);
        }

        [TestMethod]
        public void TestDrawCardLogsDrawAction()
        {
            // Arrange
            var deck = new List<int> { 28 };
            testSession.SetDrawDeck(deck);

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert
            mockLoggerHelper.Verify(x => x.LogInfo(It.Is<string>(s =>
                s.Contains("drew card") &&
                s.Contains("TEST-MATCH"))),
                Times.Once);
        }

        [TestMethod]
        public void TestDrawCardArchCardAddsToCentralBoard()
        {
            // Arrange
            var archCardId = 10; // Water Arch card
            var deck = new List<int> { archCardId };
            testSession.SetDrawDeck(deck);

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert
            Assert.AreEqual(0, testPlayer.Hand.Count);
        }

        [TestMethod]
        public void TestDrawCardArchCardNotifiesArchAdded()
        {
            // Arrange
            var archCardId = 10; // Water Arch card
            var deck = new List<int> { archCardId };
            testSession.SetDrawDeck(deck);

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyArchAddedToBoard(It.IsAny<ArchAddedToBoardDTO>()), Times.Once);
        }

        [TestMethod]
        public void TestDrawCardNonArchCardDoesNotAddToBoard()
        {
            // Arrange
            var normalCardId = 28; // Head card
            var deck = new List<int> { normalCardId };
            testSession.SetDrawDeck(deck);

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyArchAddedToBoard(It.IsAny<ArchAddedToBoardDTO>()), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestDrawCardThrowsWhenNotPlayerTurn()
        {
            // Arrange
            var deck = new List<int> { 10 };
            testSession.SetDrawDeck(deck);
            testSession.StartTurn(2); // Different player's turn

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestDrawCardThrowsWhenNoMovesRemaining()
        {
            // Arrange
            var deck = new List<int> { 28, 29, 30, 31 };
            testSession.SetDrawDeck(deck);
            testSession.ConsumeMoves(3); // Consume all moves

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestDrawCardThrowsWhenDeckIsEmpty()
        {
            // Arrange
            var deck = new List<int>();
            testSession.SetDrawDeck(deck);

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestDrawCardThrowsWhenSessionNotFound()
        {
            // Act
            gameLogic.DrawCard("INVALID-MATCH", 1);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestDrawCardThrowsWhenPlayerNotFound()
        {
            // Arrange
            var deck = new List<int> { 28 };
            testSession.SetDrawDeck(deck);

            // Act
            gameLogic.DrawCard("TEST-MATCH", 999);
        }

        [TestMethod]
        public void TestDrawCardMultipleCardsDecrementsDeck()
        {
            // Arrange
            var deck = new List<int> { 28, 29, 30, 31 };
            testSession.SetDrawDeck(deck);

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);
            testSession.RestoreMoves(1);
            gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert
            Assert.AreEqual(2, testSession.DrawDeck.Count);
        }

        [TestMethod]
        public void TestDrawCardAddsCorrectCardToHand()
        {
            // Arrange
            var deck = new List<int> { 28 };
            testSession.SetDrawDeck(deck);

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert
            Assert.AreEqual(28, testPlayer.Hand[0].IdCard);
        }

        [TestMethod]
        public void TestDrawCardWithMultiplePlayersOnlyAffectsCurrentPlayer()
        {
            // Arrange
            var player2 = new PlayerSession(2, "TestPlayer2", null);
            testSession.AddPlayer(player2);
            var deck = new List<int> { 28 };
            testSession.SetDrawDeck(deck);

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert
            Assert.AreEqual(0, player2.Hand.Count);
        }

        [TestMethod]
        public void TestDrawCardReturnsCardWithCorrectPower()
        {
            // Arrange
            var deck = new List<int> { 28 }; // Power 0
            testSession.SetDrawDeck(deck);

            // Act
            var result = gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert
            Assert.AreEqual(0, result.Power);
        }

        [TestMethod]
        public void TestDrawCardReturnsCardWithElement()
        {
            // Arrange
            var deck = new List<int> { 28 }; // Sand head
            testSession.SetDrawDeck(deck);

            // Act
            var result = gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert
            Assert.AreEqual(ArmyType.Sand, result.Element);
        }
    }
}
