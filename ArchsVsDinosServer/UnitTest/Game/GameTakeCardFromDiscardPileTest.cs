using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Board;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using ArchsVsDinosServer.Interfaces.Game;
using ArchsVsDinosServer.Utils;
using Contracts;
using Contracts.DTO.Game_DTO;
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
    public class GameTakeCardFromDiscardPileTest : BaseTestClass
    {
        private Mock<GameSessionManager> mockSessionManager;
        private Mock<GameSetupHandler> mockSetupHandler;
        private Mock<IGameNotifier> mockGameNotifier;
        private Mock<IStatisticsManager> mockStatisticsManager;
        private GameCoreContext gameCoreContext;
        private GameLogic gameLogic;
        private GameSession gameSession;
        private PlayerSession playerSession;

        [TestInitialize]
        public void SetupTakeCardFromDiscardPileTests()
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
            gameSession = new GameSession("TEST-MATCH", mockCentralBoard, mockLoggerHelper.Object);
            gameSession.MarkAsStarted();

            playerSession = new PlayerSession(1, "Player1", null);
            playerSession.TurnOrder = 1;
            var player2 = new PlayerSession(2, "Player2", null);
            player2.TurnOrder = 2;

            gameSession.AddPlayer(playerSession);
            gameSession.AddPlayer(player2);
            gameSession.StartTurn(1);

            mockSessionManager.Setup(x => x.GetSession("TEST-MATCH")).Returns(gameSession);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileAddsCardToHand()
        {
            // Arrange
            gameSession.AddToDiscard(28);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 28);

            // Assert
            Assert.AreEqual(1, playerSession.Hand.Count);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileRemovesCardFromDiscard()
        {
            // Arrange
            gameSession.AddToDiscard(28);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 28);

            // Assert
            Assert.IsFalse(gameSession.DiscardPile.Contains(28));
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileConsumesAllMoves()
        {
            // Arrange
            gameSession.AddToDiscard(28);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 28);

            // Assert
            Assert.AreEqual(0, gameSession.RemainingMoves);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileNotifiesCardTaken()
        {
            // Arrange
            gameSession.AddToDiscard(28);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 28);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyCardTakenFromDiscard(It.IsAny<CardTakenFromDiscardDTO>()), Times.Once);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileNotificationContainsCorrectMatchCode()
        {
            // Arrange
            gameSession.AddToDiscard(28);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 28);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyCardTakenFromDiscard(
                It.Is<CardTakenFromDiscardDTO>(dto => dto.MatchCode == "TEST-MATCH")),
                Times.Once);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileNotificationContainsCorrectPlayerId()
        {
            // Arrange
            gameSession.AddToDiscard(28);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 28);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyCardTakenFromDiscard(
                It.Is<CardTakenFromDiscardDTO>(dto => dto.PlayerUserId == 1)),
                Times.Once);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileNotificationContainsCorrectCardId()
        {
            // Arrange
            gameSession.AddToDiscard(28);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 28);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyCardTakenFromDiscard(
                It.Is<CardTakenFromDiscardDTO>(dto => dto.CardId == 28)),
                Times.Once);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileNotificationContainsRemainingCards()
        {
            // Arrange
            gameSession.AddToDiscard(28);
            gameSession.AddToDiscard(29);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 28);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyCardTakenFromDiscard(
                It.Is<CardTakenFromDiscardDTO>(dto => dto.RemainingCardsInDiscard == 1)),
                Times.Once);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileLogsAction()
        {
            // Arrange
            gameSession.AddToDiscard(28);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 28);

            // Assert
            mockLoggerHelper.Verify(x => x.LogInfo(It.Is<string>(s =>
                s.Contains("took card") &&
                s.Contains("from discard pile"))),
                Times.Once);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileEndsGameTurnWhenNoMovesLeft()
        {
            // Arrange
            gameSession.AddToDiscard(28);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 28);

            // Assert
            Assert.AreEqual(2, gameSession.CurrentTurn);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileNotifiesTurnChangedWhenNoMovesLeft()
        {
            // Arrange
            gameSession.AddToDiscard(28);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 28);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyTurnChanged(It.IsAny<TurnChangedDTO>()), Times.Once);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileLogsTurnAutoEnded()
        {
            // Arrange
            gameSession.AddToDiscard(28);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 28);

            // Assert
            mockLoggerHelper.Verify(x => x.LogInfo(It.Is<string>(s =>
                s.Contains("DISCARD PILE") &&
                s.Contains("Turn auto-ended"))),
                Times.Once);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileArchCardAddsToCentralBoard()
        {
            // Arrange
            gameSession.AddToDiscard(10); // Water Arch card

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 10);

            // Assert
            Assert.AreEqual(0, playerSession.Hand.Count);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileArchCardIncreasesArmyCount()
        {
            // Arrange
            gameSession.AddToDiscard(10); // Water Arch card

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 10);

            // Assert
            Assert.AreEqual(1, gameSession.CentralBoard.WaterArmy.Count);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileArchCardNotifiesArchAdded()
        {
            // Arrange
            gameSession.AddToDiscard(10); // Water Arch card

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 10);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyArchAddedToBoard(It.IsAny<ArchAddedToBoardDTO>()), Times.Once);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileNonArchCardDoesNotAddToBoard()
        {
            // Arrange
            gameSession.AddToDiscard(28); // Head card

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 28);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyArchAddedToBoard(It.IsAny<ArchAddedToBoardDTO>()), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestTakeCardFromDiscardPileThrowsWhenNotPlayerTurn()
        {
            // Arrange
            gameSession.AddToDiscard(28);
            gameSession.StartTurn(2);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 28);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestTakeCardFromDiscardPileThrowsWhenNoMovesRemaining()
        {
            // Arrange
            gameSession.AddToDiscard(28);
            gameSession.ConsumeMoves(3);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 28);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestTakeCardFromDiscardPileThrowsWhenCardNotInDiscard()
        {
            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 999);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestTakeCardFromDiscardPileThrowsWhenSessionNotFound()
        {
            // Act
            gameLogic.TakeCardFromDiscardPile("INVALID-MATCH", 1, 28);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestTakeCardFromDiscardPileThrowsWhenPlayerNotFound()
        {
            // Arrange
            gameSession.AddToDiscard(28);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 999, 28);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileWithMultipleCardsInDiscard()
        {
            // Arrange
            gameSession.AddToDiscard(28);
            gameSession.AddToDiscard(29);
            gameSession.AddToDiscard(30);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 29);

            // Assert
            Assert.AreEqual(2, gameSession.DiscardPile.Count);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileAddsCorrectCardToHand()
        {
            // Arrange
            gameSession.AddToDiscard(28);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 28);

            // Assert
            Assert.AreEqual(28, playerSession.Hand[0].IdCard);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileCyclesPlayerTurn()
        {
            // Arrange
            gameSession.AddToDiscard(28);
            gameSession.StartTurn(2);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 2, 28);

            // Assert
            Assert.AreEqual(1, gameSession.CurrentTurn);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileDoesNotEndTurnWithMovesRemaining()
        {
            // Arrange
            gameSession.AddToDiscard(28);
            gameSession.ConsumeMoves(1);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 28);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyTurnChanged(It.IsAny<TurnChangedDTO>()), Times.Never);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileTurnChangedNotificationContainsPlayerScores()
        {
            // Arrange
            gameSession.AddToDiscard(28);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 28);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyTurnChanged(
                It.Is<TurnChangedDTO>(dto => dto.PlayerScores != null && dto.PlayerScores.Count == 2)),
                Times.Once);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileWithSandArchCard()
        {
            // Arrange
            gameSession.AddToDiscard(1); // Sand Arch card

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 1);

            // Assert
            Assert.AreEqual(1, gameSession.CentralBoard.SandArmy.Count);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileWithWindArchCard()
        {
            // Arrange
            gameSession.AddToDiscard(19); // Wind Arch card

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 19);

            // Assert
            Assert.AreEqual(1, gameSession.CentralBoard.WindArmy.Count);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileLogsMovesConsumed()
        {
            // Arrange
            gameSession.AddToDiscard(28);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 28);

            // Assert
            mockLoggerHelper.Verify(x => x.LogInfo(It.Is<string>(s =>
                s.Contains("Moves consumed"))),
                Times.Once);
        }

        [TestMethod]
        public void TestTakeCardFromDiscardPileEmptiesDiscardWhenLastCard()
        {
            // Arrange
            gameSession.AddToDiscard(28);

            // Act
            gameLogic.TakeCardFromDiscardPile("TEST-MATCH", 1, 28);

            // Assert
            Assert.AreEqual(0, gameSession.DiscardPile.Count);
        }
    }
}
