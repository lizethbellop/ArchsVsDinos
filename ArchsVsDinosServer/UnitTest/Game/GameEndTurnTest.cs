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
    public class GameEndTurnTest : BaseTestClass
    {
        private Mock<GameSessionManager> mockSessionManager;
        private Mock<GameSetupHandler> mockSetupHandler;
        private Mock<IGameNotifier> mockGameNotifier;
        private Mock<IStatisticsManager> mockStatisticsManager;
        private GameCoreContext gameCoreContext;
        private GameLogic gameLogic;
        private GameSession gameSession;
        private PlayerSession currentPlayer;

        [TestInitialize]
        public void SetupEndTurnTests()
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

            currentPlayer = new PlayerSession(1, "Player1", null);
            currentPlayer.TurnOrder = 1;
            var player2 = new PlayerSession(2, "Player2", null);
            player2.TurnOrder = 2;

            gameSession.AddPlayer(currentPlayer);
            gameSession.AddPlayer(player2);
            gameSession.StartTurn(1);

            mockSessionManager.Setup(x => x.GetSession("TEST-MATCH")).Returns(gameSession);
        }

        [TestMethod]
        public void TestEndTurnReturnsTrue()
        {
            // Act
            var result = gameLogic.EndTurn("TEST-MATCH", 1);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestEndTurnChangesCurrentPlayer()
        {
            // Act
            gameLogic.EndTurn("TEST-MATCH", 1);

            // Assert
            Assert.AreEqual(2, gameSession.CurrentTurn);
        }

        [TestMethod]
        public void TestEndTurnResetsMovesToMax()
        {
            // Arrange
            gameSession.ConsumeMoves(2);

            // Act
            gameLogic.EndTurn("TEST-MATCH", 1);

            // Assert
            Assert.AreEqual(GameSession.MaxMoves, gameSession.RemainingMoves);
        }

        [TestMethod]
        public void TestEndTurnNotifiesTurnChanged()
        {
            // Act
            gameLogic.EndTurn("TEST-MATCH", 1);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyTurnChanged(It.IsAny<TurnChangedDTO>()), Times.Once);
        }

        [TestMethod]
        public void TestEndTurnNotificationContainsCorrectMatchCode()
        {
            // Act
            gameLogic.EndTurn("TEST-MATCH", 1);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyTurnChanged(
                It.Is<TurnChangedDTO>(dto => dto.MatchCode == "TEST-MATCH")),
                Times.Once);
        }

        [TestMethod]
        public void TestEndTurnNotificationContainsNextPlayerId()
        {
            // Act
            gameLogic.EndTurn("TEST-MATCH", 1);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyTurnChanged(
                It.Is<TurnChangedDTO>(dto => dto.CurrentPlayerUserId == 2)),
                Times.Once);
        }

        [TestMethod]
        public void TestEndTurnNotificationContainsTurnNumber()
        {
            // Arrange
            int expectedTurnNumber = gameSession.TurnNumber;

            // Act
            gameLogic.EndTurn("TEST-MATCH", 1);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyTurnChanged(
                It.Is<TurnChangedDTO>(dto => dto.TurnNumber == expectedTurnNumber)),
                Times.Once);
        }

        [TestMethod]
        public void TestEndTurnNotificationContainsPlayerScores()
        {
            // Act
            gameLogic.EndTurn("TEST-MATCH", 1);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyTurnChanged(
                It.Is<TurnChangedDTO>(dto => dto.PlayerScores != null && dto.PlayerScores.Count == 2)),
                Times.Once);
        }

        [TestMethod]
        public void TestEndTurnLogsAction()
        {
            // Act
            gameLogic.EndTurn("TEST-MATCH", 1);

            // Assert
            mockLoggerHelper.Verify(x => x.LogInfo(It.Is<string>(s =>
                s.Contains("Turn ended") &&
                s.Contains("TEST-MATCH"))),
                Times.Once);
        }

        [TestMethod]
        public void TestEndTurnWithThreePlayersCyclesToNextPlayer()
        {
            // Arrange
            var player3 = new PlayerSession(3, "Player3", null);
            player3.TurnOrder = 3;
            gameSession.AddPlayer(player3);

            // Act
            gameLogic.EndTurn("TEST-MATCH", 1);

            // Assert
            Assert.AreEqual(2, gameSession.CurrentTurn);
        }

        [TestMethod]
        public void TestEndTurnFromLastPlayerCyclesToFirst()
        {
            // Arrange
            gameSession.StartTurn(2);

            // Act
            gameLogic.EndTurn("TEST-MATCH", 2);

            // Assert
            Assert.AreEqual(1, gameSession.CurrentTurn);
        }

        [TestMethod]
        public void TestEndTurnWithFourPlayersCyclesToNextPlayer()
        {
            // Arrange
            var player3 = new PlayerSession(3, "Player3", null);
            player3.TurnOrder = 3;
            var player4 = new PlayerSession(4, "Player4", null);
            player4.TurnOrder = 4;
            gameSession.AddPlayer(player3);
            gameSession.AddPlayer(player4);

            // Act
            gameLogic.EndTurn("TEST-MATCH", 1);

            // Assert
            Assert.AreEqual(2, gameSession.CurrentTurn);
        }

        [TestMethod]
        public void TestEndTurnFromPlayer2GoesToPlayer3()
        {
            // Arrange
            var player3 = new PlayerSession(3, "Player3", null);
            player3.TurnOrder = 3;
            gameSession.AddPlayer(player3);
            gameSession.StartTurn(2);

            // Act
            gameLogic.EndTurn("TEST-MATCH", 2);

            // Assert
            Assert.AreEqual(3, gameSession.CurrentTurn);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestEndTurnThrowsWhenPlayerNotFound()
        {
            // Act
            gameLogic.EndTurn("TEST-MATCH", 999);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestEndTurnThrowsWhenSessionNotFound()
        {
            // Act
            gameLogic.EndTurn("INVALID-MATCH", 1);
        }

        [TestMethod]
        public void TestEndTurnMaintainsPlayerScores()
        {
            // Arrange
            currentPlayer.Points = 10;

            // Act
            gameLogic.EndTurn("TEST-MATCH", 1);

            // Assert
            Assert.AreEqual(10, currentPlayer.Points);
        }

        [TestMethod]
        public void TestEndTurnCanBeCalledMultipleTimes()
        {
            // Act
            gameLogic.EndTurn("TEST-MATCH", 1);
            gameLogic.EndTurn("TEST-MATCH", 2);

            // Assert
            Assert.AreEqual(1, gameSession.CurrentTurn);
        }

        [TestMethod]
        public void TestEndTurnNotificationContainsAllPlayerScores()
        {
            // Arrange
            currentPlayer.Points = 5;
            gameSession.Players.First(p => p.UserId == 2).Points = 8;

            // Act
            gameLogic.EndTurn("TEST-MATCH", 1);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyTurnChanged(
                It.Is<TurnChangedDTO>(dto =>
                    dto.PlayerScores.ContainsKey(1) &&
                    dto.PlayerScores.ContainsKey(2))),
                Times.Once);
        }

        [TestMethod]
        public void TestEndTurnPreservesPlayerHands()
        {
            // Arrange
            var card = CreateHeadCard(28, 0, ArmyType.Sand);
            currentPlayer.AddCard(card);

            // Act
            gameLogic.EndTurn("TEST-MATCH", 1);

            // Assert
            Assert.AreEqual(1, currentPlayer.Hand.Count);
        }

        [TestMethod]
        public void TestEndTurnPreservesPlayerDinos()
        {
            // Arrange
            var headCard = CreateHeadCard(28, 0, ArmyType.Sand);
            var dino = new DinoInstance(1, headCard);
            currentPlayer.AddDino(dino);

            // Act
            gameLogic.EndTurn("TEST-MATCH", 1);

            // Assert
            Assert.AreEqual(1, currentPlayer.Dinos.Count);
        }

        [TestMethod]
        public void TestEndTurnWithTwoPlayersAlternatesCorrectly()
        {
            // Act
            gameLogic.EndTurn("TEST-MATCH", 1);
            gameLogic.EndTurn("TEST-MATCH", 2);
            gameLogic.EndTurn("TEST-MATCH", 1);

            // Assert
            Assert.AreEqual(2, gameSession.CurrentTurn);
        }

        [TestMethod]
        public void TestEndTurnNotifiesOnlyOnce()
        {
            // Act
            gameLogic.EndTurn("TEST-MATCH", 1);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyTurnChanged(It.IsAny<TurnChangedDTO>()), Times.Once);
        }

        [TestMethod]
        public void TestEndTurnLogsNextPlayerId()
        {
            // Act
            gameLogic.EndTurn("TEST-MATCH", 1);

            // Assert
            mockLoggerHelper.Verify(x => x.LogInfo(It.Is<string>(s =>
                s.Contains("Next: 2"))),
                Times.Once);
        }

        private CardInGame CreateHeadCard(int id, int power, ArmyType element)
        {
            var card = new CardInGame();
            var type = typeof(CardInGame);
            type.GetProperty("IdCard").SetValue(card, id, null);
            type.GetProperty("Power").SetValue(card, power, null);
            type.GetProperty("Element").SetValue(card, element, null);
            type.GetProperty("PartType").SetValue(card, DinoPartType.Head, null);
            type.GetProperty("HasBottomJoint").SetValue(card, true, null);
            return card;
        }
    }
}
