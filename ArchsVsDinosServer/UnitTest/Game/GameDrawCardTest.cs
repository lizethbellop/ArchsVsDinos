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

namespace UnitTest.Game
{
    [TestClass]
    public class GameDrawCardTest : BaseTestClass
    {
        // YA NO MOCKEAMOS EL SESSION MANAGER porque sus métodos no son virtuales
        private GameSessionManager sessionManager;
        private Mock<IGameNotifier> mockGameNotifier;
        private Mock<IStatisticsManager> mockStatisticsManager;
        private Mock<GameSetupHandler> mockSetupHandler;
        private GameCoreContext gameCoreContext;
        private GameLogic gameLogic;
        private GameSession testSession;
        private PlayerSession testPlayer;

        [TestInitialize]
        public void SetupDrawCardTests()
        {
            base.BaseSetup();

            sessionManager = new GameSessionManager(mockLoggerHelper.Object);
            mockGameNotifier = new Mock<IGameNotifier>();
            mockStatisticsManager = new Mock<IStatisticsManager>();
            mockSetupHandler = new Mock<GameSetupHandler>();

            gameCoreContext = new GameCoreContext(sessionManager, mockSetupHandler.Object);

            var dependencies = new GameLogicDependencies(
                gameCoreContext,
                mockLoggerHelper.Object,
                mockGameNotifier.Object,
                mockStatisticsManager.Object
            );

            gameLogic = new GameLogic(dependencies);

            string matchCode = "TEST-MATCH";
            sessionManager.CreateSession(matchCode);
            testSession = sessionManager.GetSession(matchCode);

            testSession.MarkAsStarted();

            testPlayer = new PlayerSession(1, "TestPlayer", null);
            testSession.AddPlayer(testPlayer);
            testSession.StartTurn(1);
        }

        [TestMethod]
        public void TestDrawCard_Success_ReturnsCard()
        {
            // Arrange
            testSession.SetDrawDeck(new List<int> { 28 }); 

            // Act
            var result = gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(28, result.IdCard);
            Assert.AreEqual(1, testPlayer.Hand.Count);
        }

        [TestMethod]
        public void TestDrawCard_ArchCard_AddsToBoard()
        {
            // Arrange: 
            testSession.SetDrawDeck(new List<int> { 10 });

            // Act
            var result = gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert
            Assert.AreEqual(0, testPlayer.Hand.Count, "Arch card should not go to hand");
            Assert.IsTrue(testSession.CentralBoard.WaterArmy.Count > 0, "Arch should be added to Board");
            mockGameNotifier.Verify(n => n.NotifyArchAddedToBoard(It.IsAny<ArchAddedToBoardDTO>()), Times.Once);
        }

        [TestMethod]
        public void TestDrawCard_ConsumesOneMove()
        {
            // Arrange
            testSession.SetDrawDeck(new List<int> { 28 });
            int movesBefore = testSession.RemainingMoves;

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert
            Assert.AreEqual(movesBefore - 1, testSession.RemainingMoves);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestDrawCard_Throws_WhenDeckIsEmpty()
        {
            // Arrange
            testSession.SetDrawDeck(new List<int>());

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);
        }

        [TestMethod]
        public void TestDrawCard_TriggersGameOver_WhenLastCardDrawn()
        {
            // Arrange
            testSession.SetDrawDeck(new List<int> { 28 });

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert
            Assert.IsTrue(testSession.IsFinished, "Match should end when deck is empty");
            mockGameNotifier.Verify(n => n.NotifyGameEnded(It.IsAny<GameEndedDTO>()), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestDrawCard_Throws_WhenNotPlayerTurn()
        {
            // Arrange
            testSession.SetDrawDeck(new List<int> { 28 });
            testSession.StartTurn(99);

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestDrawCard_Throws_WhenNoMovesRemaining()
        {
            // Arrange
            testSession.SetDrawDeck(new List<int> { 28, 29, 30, 31 });
            testSession.ConsumeMoves(testSession.RemainingMoves);

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);
        }

        [TestMethod]
        public void TestDrawCard_Notification_ContainsCorrectData()
        {
            // Arrange
            int expectedCardId = 28;
            testSession.SetDrawDeck(new List<int> { expectedCardId });

            // Act
            gameLogic.DrawCard("TEST-MATCH", 1);

            // Assert: 
            mockGameNotifier.Verify(n => n.NotifyCardDrawn(It.Is<CardDrawnDTO>(dto =>
                dto.MatchCode == "TEST-MATCH" &&
                dto.PlayerUserId == 1 &&
                dto.Card.IdCard == expectedCardId
            )), Times.Once);
        }
    }
}