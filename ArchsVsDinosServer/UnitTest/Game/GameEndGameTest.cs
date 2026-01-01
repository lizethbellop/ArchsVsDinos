using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Board;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using ArchsVsDinosServer.Interfaces.Game;
using ArchsVsDinosServer.Model;
using ArchsVsDinosServer.Utils;
using Contracts;
using Contracts.DTO.Game_DTO;
using Contracts.DTO.Result_Codes;
using Contracts.DTO.Statistics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.Game
{
    [TestClass]
    public class GameLogicEndGameTests : BaseTestClass
    {
        private Mock<GameSessionManager> mockSessionManager;
        private Mock<GameSetupHandler> mockSetupHandler;
        private Mock<GameEndHandler> mockGameEndHandler;
        private Mock<IGameNotifier> mockGameNotifier;
        private Mock<IStatisticsManager> mockStatisticsManager;
        private GameCoreContext gameCoreContext;
        private GameLogic gameLogic;
        private GameSession gameSession;
        private PlayerSession playerA;
        private PlayerSession playerB;

        [TestInitialize]
        public void SetupEndGameTests()
        {
            BaseSetup();

            mockSessionManager = new Mock<GameSessionManager>(mockLoggerHelper.Object);
            mockSetupHandler = new Mock<GameSetupHandler>();
            mockGameEndHandler = new Mock<GameEndHandler>();
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

            playerA = new PlayerSession(1, "PlayerA", null);
            playerB = new PlayerSession(2, "PlayerB", null);
            playerA.Points = 100;
            playerB.Points = 80;

            gameSession.AddPlayer(playerA);
            gameSession.AddPlayer(playerB);

            mockSessionManager.Setup(x => x.GetSession("TEST-MATCH")).Returns(gameSession);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestEndGameThrowsExceptionWhenMatchCodeIsNull()
        {
            gameLogic.EndGame(null, GameEndType.Finished, "test reason");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestEndGameThrowsExceptionWhenMatchCodeIsEmpty()
        {
            gameLogic.EndGame("", GameEndType.Finished, "test reason");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestEndGameThrowsExceptionWhenMatchCodeIsWhitespace()
        {
            gameLogic.EndGame("   ", GameEndType.Finished, "test reason");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestEndGameThrowsExceptionWhenSessionNotFound()
        {
            mockSessionManager.Setup(x => x.GetSession("INVALID")).Returns((GameSession)null);

            gameLogic.EndGame("INVALID", GameEndType.Finished, "test reason");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestEndGameThrowsExceptionWhenGameShouldNotEnd()
        {
            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(false);

            gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestEndGameThrowsExceptionWhenResultIsNull()
        {
            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(gameSession)).Returns((GameEndResult)null);

            gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");
        }

        [TestMethod]
        public void TestEndGameReturnsResultWithCorrectWinner()
        {
            var expectedResult = new GameEndResult
            {
                Winner = playerA,
                WinnerPoints = 100,
                Reason = "cards_depleted"
            };

            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(gameSession)).Returns(expectedResult);
            mockStatisticsManager.Setup(x => x.SaveMatchStatistics(It.IsAny<MatchResultDTO>()))
                .Returns(SaveMatchResultCode.Success);

            var result = gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");

            Assert.AreEqual(playerA, result.Winner);
        }

        [TestMethod]
        public void TestEndGameReturnsResultWithCorrectWinnerPoints()
        {
            var expectedResult = new GameEndResult
            {
                Winner = playerA,
                WinnerPoints = 100,
                Reason = "cards_depleted"
            };

            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(gameSession)).Returns(expectedResult);
            mockStatisticsManager.Setup(x => x.SaveMatchStatistics(It.IsAny<MatchResultDTO>()))
                .Returns(SaveMatchResultCode.Success);

            var result = gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");

            Assert.AreEqual(100, result.WinnerPoints);
        }

        [TestMethod]
        public void TestEndGameMarksSessionAsFinished()
        {
            var gameResult = new GameEndResult
            {
                Winner = playerA,
                WinnerPoints = 100,
                Reason = "cards_depleted"
            };

            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(gameSession)).Returns(gameResult);
            mockStatisticsManager.Setup(x => x.SaveMatchStatistics(It.IsAny<MatchResultDTO>()))
                .Returns(SaveMatchResultCode.Success);

            gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");

            Assert.IsTrue(gameSession.IsFinished);
        }

        [TestMethod]
        public void TestEndGameSetsCorrectEndType()
        {
            var gameResult = new GameEndResult
            {
                Winner = playerA,
                WinnerPoints = 100,
                Reason = "cards_depleted"
            };

            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(gameSession)).Returns(gameResult);
            mockStatisticsManager.Setup(x => x.SaveMatchStatistics(It.IsAny<MatchResultDTO>()))
                .Returns(SaveMatchResultCode.Success);

            gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");

            Assert.AreEqual(GameEndType.Finished, gameSession.EndType);
        }

        [TestMethod]
        public void TestEndGameRemovesSession()
        {
            var gameResult = new GameEndResult
            {
                Winner = playerA,
                WinnerPoints = 100,
                Reason = "cards_depleted"
            };

            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(gameSession)).Returns(gameResult);
            mockStatisticsManager.Setup(x => x.SaveMatchStatistics(It.IsAny<MatchResultDTO>()))
                .Returns(SaveMatchResultCode.Success);

            gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");

            mockSessionManager.Verify(x => x.RemoveSession("TEST-MATCH"), Times.Once);
        }

        [TestMethod]
        public void TestEndGameNotifiesGameEnded()
        {
            var gameResult = new GameEndResult
            {
                Winner = playerA,
                WinnerPoints = 100,
                Reason = "cards_depleted"
            };

            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(gameSession)).Returns(gameResult);
            mockStatisticsManager.Setup(x => x.SaveMatchStatistics(It.IsAny<MatchResultDTO>()))
                .Returns(SaveMatchResultCode.Success);

            gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");

            mockGameNotifier.Verify(x => x.NotifyGameEnded(It.IsAny<GameEndedDTO>()), Times.Once);
        }

        [TestMethod]
        public void TestEndGameNotificationContainsCorrectMatchCode()
        {
            var gameResult = new GameEndResult
            {
                Winner = playerA,
                WinnerPoints = 100,
                Reason = "cards_depleted"
            };

            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(gameSession)).Returns(gameResult);
            mockStatisticsManager.Setup(x => x.SaveMatchStatistics(It.IsAny<MatchResultDTO>()))
                .Returns(SaveMatchResultCode.Success);

            gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");

            mockGameNotifier.Verify(x => x.NotifyGameEnded(
                It.Is<GameEndedDTO>(dto => dto.MatchCode == "TEST-MATCH")), Times.Once);
        }

        [TestMethod]
        public void TestEndGameNotificationContainsCorrectWinnerUserId()
        {
            var gameResult = new GameEndResult
            {
                Winner = playerA,
                WinnerPoints = 100,
                Reason = "cards_depleted"
            };

            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(gameSession)).Returns(gameResult);
            mockStatisticsManager.Setup(x => x.SaveMatchStatistics(It.IsAny<MatchResultDTO>()))
                .Returns(SaveMatchResultCode.Success);

            gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");

            mockGameNotifier.Verify(x => x.NotifyGameEnded(
                It.Is<GameEndedDTO>(dto => dto.WinnerUserId == 1)), Times.Once);
        }

        [TestMethod]
        public void TestEndGameNotificationContainsCorrectReason()
        {
            var gameResult = new GameEndResult
            {
                Winner = playerA,
                WinnerPoints = 100,
                Reason = "cards_depleted"
            };

            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(gameSession)).Returns(gameResult);
            mockStatisticsManager.Setup(x => x.SaveMatchStatistics(It.IsAny<MatchResultDTO>()))
                .Returns(SaveMatchResultCode.Success);

            gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");

            mockGameNotifier.Verify(x => x.NotifyGameEnded(
                It.Is<GameEndedDTO>(dto => dto.Reason == "test reason")), Times.Once);
        }

        [TestMethod]
        public void TestEndGameNotificationContainsCorrectWinnerPoints()
        {
            var gameResult = new GameEndResult
            {
                Winner = playerA,
                WinnerPoints = 100,
                Reason = "cards_depleted"
            };

            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(gameSession)).Returns(gameResult);
            mockStatisticsManager.Setup(x => x.SaveMatchStatistics(It.IsAny<MatchResultDTO>()))
                .Returns(SaveMatchResultCode.Success);

            gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");

            mockGameNotifier.Verify(x => x.NotifyGameEnded(
                It.Is<GameEndedDTO>(dto => dto.WinnerPoints == 100)), Times.Once);
        }

        [TestMethod]
        public void TestEndGameAbortedCreatesResultWithNullWinner()
        {
            var result = gameLogic.EndGame("TEST-MATCH", GameEndType.Aborted, "player disconnected");

            Assert.IsNull(result.Winner);
        }

        [TestMethod]
        public void TestEndGameAbortedSetsWinnerPointsToZero()
        {
            var result = gameLogic.EndGame("TEST-MATCH", GameEndType.Aborted, "player disconnected");

            Assert.AreEqual(0, result.WinnerPoints);
        }

        [TestMethod]
        public void TestEndGameAbortedSetsCorrectReason()
        {
            var result = gameLogic.EndGame("TEST-MATCH", GameEndType.Aborted, "player disconnected");

            Assert.AreEqual("player disconnected", result.Reason);
        }

        [TestMethod]
        public void TestEndGameAbortedDoesNotSaveStatistics()
        {
            gameLogic.EndGame("TEST-MATCH", GameEndType.Aborted, "player disconnected");

            mockStatisticsManager.Verify(x => x.SaveMatchStatistics(It.IsAny<MatchResultDTO>()), Times.Never);
        }

        [TestMethod]
        public void TestEndGameAbortedMarksSessionAsFinished()
        {
            gameLogic.EndGame("TEST-MATCH", GameEndType.Aborted, "player disconnected");

            Assert.IsTrue(gameSession.IsFinished);
        }

        [TestMethod]
        public void TestEndGameAbortedNotifiesWithZeroUserId()
        {
            gameLogic.EndGame("TEST-MATCH", GameEndType.Aborted, "player disconnected");

            mockGameNotifier.Verify(x => x.NotifyGameEnded(
                It.Is<GameEndedDTO>(dto => dto.WinnerUserId == 0)), Times.Once);
        }

        [TestMethod]
        public void TestEndGameContinuesExecutionWhenSqlExceptionOccurs()
        {
            var gameResult = new GameEndResult
            {
                Winner = playerA,
                WinnerPoints = 100,
                Reason = "cards_depleted"
            };

            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(gameSession)).Returns(gameResult);
            mockStatisticsManager.Setup(x => x.SaveMatchStatistics(It.IsAny<MatchResultDTO>()))
                .Throws(new Exception("SQL Error"));

            var result = gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestEndGameAppendsReasonWhenSqlExceptionOccurs()
        {
            var gameResult = new GameEndResult
            {
                Winner = playerA,
                WinnerPoints = 100,
                Reason = "cards_depleted"
            };

            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(gameSession)).Returns(gameResult);
            mockStatisticsManager.Setup(x => x.SaveMatchStatistics(It.IsAny<MatchResultDTO>()))
                .Throws(new Exception("SQL Error"));

            var result = gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");

            Assert.IsTrue(result.Reason.Contains("statistics_sql_error"));
        }

        [TestMethod]
        public void TestEndGameAppendsReasonWhenEntityExceptionOccurs()
        {
            var gameResult = new GameEndResult
            {
                Winner = playerA,
                WinnerPoints = 100,
                Reason = "time_expired"
            };

            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(gameSession)).Returns(gameResult);
            mockStatisticsManager.Setup(x => x.SaveMatchStatistics(It.IsAny<MatchResultDTO>()))
                .Throws(new EntityException());

            var result = gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");

            Assert.IsTrue(result.Reason.Contains("statistics_entity_error"));
        }

        [TestMethod]
        public void TestEndGameAppendsReasonWhenArgumentNullExceptionOccurs()
        {
            var gameResult = new GameEndResult
            {
                Winner = playerA,
                WinnerPoints = 100,
                Reason = "cards_depleted"
            };

            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(gameSession)).Returns(gameResult);
            mockStatisticsManager.Setup(x => x.SaveMatchStatistics(It.IsAny<MatchResultDTO>()))
                .Throws(new ArgumentNullException());

            var result = gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");

            Assert.IsTrue(result.Reason.Contains("statistics_null_error"));
        }

        [TestMethod]
        public void TestEndGameAppendsReasonWhenInvalidOperationExceptionOccurs()
        {
            var gameResult = new GameEndResult
            {
                Winner = playerA,
                WinnerPoints = 100,
                Reason = "cards_depleted"
            };

            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(gameSession)).Returns(gameResult);
            mockStatisticsManager.Setup(x => x.SaveMatchStatistics(It.IsAny<MatchResultDTO>()))
                .Throws(new InvalidOperationException("Statistics error"));

            var result = gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");

            Assert.IsTrue(result.Reason.Contains("statistics_invalid_operation"));
        }

        [TestMethod]
        public void TestEndGameDoesNotSaveStatisticsWhenNoRegisteredPlayers()
        {
            var guestSession = new GameSession("GUEST-MATCH", new CentralBoard(), mockLoggerHelper.Object);
            guestSession.MarkAsStarted();

            var guestA = new PlayerSession(0, "GuestA", null);
            var guestB = new PlayerSession(0, "GuestB", null);
            guestSession.AddPlayer(guestA);
            guestSession.AddPlayer(guestB);

            mockSessionManager.Setup(x => x.GetSession("GUEST-MATCH")).Returns(guestSession);

            var gameResult = new GameEndResult
            {
                Winner = guestA,
                WinnerPoints = 50,
                Reason = "cards_depleted"
            };

            mockGameEndHandler.Setup(x => x.ShouldGameEnd(guestSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(guestSession)).Returns(gameResult);

            gameLogic.EndGame("GUEST-MATCH", GameEndType.Finished, "test reason");

            mockStatisticsManager.Verify(x => x.SaveMatchStatistics(It.IsAny<MatchResultDTO>()), Times.Never);
        }

        [TestMethod]
        public void TestEndGameNotifiesWithZeroUserIdWhenWinnerIsNull()
        {
            var gameResult = new GameEndResult
            {
                Winner = null,
                WinnerPoints = 0,
                Reason = "cards_depleted"
            };

            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(gameSession)).Returns(gameResult);

            gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");

            mockGameNotifier.Verify(x => x.NotifyGameEnded(
                It.Is<GameEndedDTO>(dto => dto.WinnerUserId == 0)), Times.Once);
        }

        [TestMethod]
        public void TestEndGameStillNotifiesWhenStatisticsSaveFails()
        {
            var gameResult = new GameEndResult
            {
                Winner = playerA,
                WinnerPoints = 100,
                Reason = "cards_depleted"
            };

            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(gameSession)).Returns(gameResult);
            mockStatisticsManager.Setup(x => x.SaveMatchStatistics(It.IsAny<MatchResultDTO>()))
                .Throws(new Exception("SQL Error"));

            gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");

            mockGameNotifier.Verify(x => x.NotifyGameEnded(It.IsAny<GameEndedDTO>()), Times.Once);
        }

        [TestMethod]
        public void TestEndGameStillRemovesSessionWhenStatisticsSaveFails()
        {
            var gameResult = new GameEndResult
            {
                Winner = playerA,
                WinnerPoints = 100,
                Reason = "cards_depleted"
            };

            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(gameSession)).Returns(gameResult);
            mockStatisticsManager.Setup(x => x.SaveMatchStatistics(It.IsAny<MatchResultDTO>()))
                .Throws(new Exception("SQL Error"));

            gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");

            mockSessionManager.Verify(x => x.RemoveSession("TEST-MATCH"), Times.Once);
        }

        [TestMethod]
        public void TestEndGameSetsOnlyNewReasonWhenOriginalReasonIsEmpty()
        {
            var gameResult = new GameEndResult
            {
                Winner = playerA,
                WinnerPoints = 100,
                Reason = ""
            };

            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(gameSession)).Returns(gameResult);
            mockStatisticsManager.Setup(x => x.SaveMatchStatistics(It.IsAny<MatchResultDTO>()))
                .Throws(new Exception("SQL Error"));

            var result = gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");

            Assert.AreEqual("statistics_sql_error", result.Reason);
        }

        [TestMethod]
        public void TestEndGameConcatenatesReasonsWithSemicolon()
        {
            var gameResult = new GameEndResult
            {
                Winner = playerA,
                WinnerPoints = 100,
                Reason = "cards_depleted"
            };

            mockGameEndHandler.Setup(x => x.ShouldGameEnd(gameSession)).Returns(true);
            mockGameEndHandler.Setup(x => x.EndGame(gameSession)).Returns(gameResult);
            mockStatisticsManager.Setup(x => x.SaveMatchStatistics(It.IsAny<MatchResultDTO>()))
                .Throws(new Exception("SQL Error"));

            var result = gameLogic.EndGame("TEST-MATCH", GameEndType.Finished, "test reason");

            Assert.IsTrue(result.Reason.Contains("cards_depleted;statistics_sql_error"));
        }
    }
}
