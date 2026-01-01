using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Board;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using ArchsVsDinosServer.Interfaces.Game;
using ArchsVsDinosServer.Utils;
using Contracts;
using Contracts.DTO;
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
    public class GameInitializeMatchTest : BaseTestClass
    {
        private Mock<GameSessionManager> mockSessionManager;
        private Mock<GameSetupHandler> mockSetupHandler;
        private Mock<IGameNotifier> mockGameNotifier;
        private Mock<IStatisticsManager> mockStatisticsManager;
        private GameCoreContext gameCoreContext;
        private GameLogic gameLogic;

        [TestInitialize]
        public void SetupInitializeMatchTests()
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

            mockSessionManager.Setup(x => x.CreateSession(It.IsAny<string>())).Returns(true);
            mockSessionManager.Setup(x => x.SessionExists(It.IsAny<string>())).Returns(false);
            mockSetupHandler.Setup(x => x.InitializeGameSession(It.IsAny<GameSession>(), It.IsAny<List<PlayerSession>>())).Returns(true);
            mockSetupHandler.Setup(x => x.SelectFirstPlayer(It.IsAny<GameSession>())).Returns((GameSession session) => session.Players.FirstOrDefault());
        }

        [TestMethod]
        public async Task TestInitializeMatchReturnsTrue()
        {
            // Arrange
            var mockCentralBoard = new CentralBoard();
            var testSession = new GameSession("TEST-001", mockCentralBoard, mockLoggerHelper.Object);
            mockSessionManager.Setup(x => x.GetSession("TEST-001")).Returns(testSession);

            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" },
            new GamePlayerInitDTO { UserId = 2, Nickname = "Player2" }
        };

            // Act
            var result = await gameLogic.InitializeMatch("TEST-001", players);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestInitializeMatchCreatesSession()
        {
            // Arrange
            var mockCentralBoard = new CentralBoard();
            var testSession = new GameSession("TEST-001", mockCentralBoard, mockLoggerHelper.Object);
            mockSessionManager.Setup(x => x.GetSession("TEST-001")).Returns(testSession);

            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" },
            new GamePlayerInitDTO { UserId = 2, Nickname = "Player2" }
        };

            // Act
            await gameLogic.InitializeMatch("TEST-001", players);

            // Assert
            mockSessionManager.Verify(x => x.CreateSession("TEST-001"), Times.Once);
        }

        [TestMethod]
        public async Task TestInitializeMatchMarksSessionAsStarted()
        {
            // Arrange
            var mockCentralBoard = new CentralBoard();
            var testSession = new GameSession("TEST-001", mockCentralBoard, mockLoggerHelper.Object);
            mockSessionManager.Setup(x => x.GetSession("TEST-001")).Returns(testSession);

            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" },
            new GamePlayerInitDTO { UserId = 2, Nickname = "Player2" }
        };

            // Act
            await gameLogic.InitializeMatch("TEST-001", players);

            // Assert
            Assert.IsTrue(testSession.IsStarted);
        }

        [TestMethod]
        public async Task TestInitializeMatchNotifiesGameInitialized()
        {
            // Arrange
            var mockCentralBoard = new CentralBoard();
            var testSession = new GameSession("TEST-001", mockCentralBoard, mockLoggerHelper.Object);
            mockSessionManager.Setup(x => x.GetSession("TEST-001")).Returns(testSession);

            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" },
            new GamePlayerInitDTO { UserId = 2, Nickname = "Player2" }
        };

            // Act
            await gameLogic.InitializeMatch("TEST-001", players);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyGameInitialized(It.IsAny<GameInitializedDTO>()), Times.Once);
        }

        [TestMethod]
        public async Task TestInitializeMatchNotificationContainsCorrectMatchCode()
        {
            // Arrange
            var mockCentralBoard = new CentralBoard();
            var testSession = new GameSession("TEST-001", mockCentralBoard, mockLoggerHelper.Object);
            mockSessionManager.Setup(x => x.GetSession("TEST-001")).Returns(testSession);

            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" },
            new GamePlayerInitDTO { UserId = 2, Nickname = "Player2" }
        };

            // Act
            await gameLogic.InitializeMatch("TEST-001", players);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyGameInitialized(
                It.Is<GameInitializedDTO>(dto => dto.MatchCode == "TEST-001")),
                Times.Once);
        }

        [TestMethod]
        public async Task TestInitializeMatchNotificationContainsCorrectPlayerCount()
        {
            // Arrange
            var mockCentralBoard = new CentralBoard();
            var testSession = new GameSession("TEST-001", mockCentralBoard, mockLoggerHelper.Object);
            mockSessionManager.Setup(x => x.GetSession("TEST-001")).Returns(testSession);

            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" },
            new GamePlayerInitDTO { UserId = 2, Nickname = "Player2" }
        };

            // Act
            await gameLogic.InitializeMatch("TEST-001", players);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyGameInitialized(
                It.Is<GameInitializedDTO>(dto => dto.Players.Count == 2)),
                Times.Once);
        }

        [TestMethod]
        public async Task TestInitializeMatchNotifiesGameStartedToEachPlayer()
        {
            // Arrange
            var mockCentralBoard = new CentralBoard();
            var testSession = new GameSession("TEST-001", mockCentralBoard, mockLoggerHelper.Object);
            mockSessionManager.Setup(x => x.GetSession("TEST-001")).Returns(testSession);

            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" },
            new GamePlayerInitDTO { UserId = 2, Nickname = "Player2" }
        };

            // Act
            await gameLogic.InitializeMatch("TEST-001", players);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyGameStarted(It.IsAny<GameStartedDTO>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task TestInitializeMatchLogsSuccess()
        {
            // Arrange
            var mockCentralBoard = new CentralBoard();
            var testSession = new GameSession("TEST-001", mockCentralBoard, mockLoggerHelper.Object);
            mockSessionManager.Setup(x => x.GetSession("TEST-001")).Returns(testSession);

            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" },
            new GamePlayerInitDTO { UserId = 2, Nickname = "Player2" }
        };

            // Act
            await gameLogic.InitializeMatch("TEST-001", players);

            // Assert
            mockLoggerHelper.Verify(x => x.LogInfo(It.Is<string>(s =>
                s.Contains("initialized successfully"))),
                Times.Once);
        }

        [TestMethod]
        public async Task TestInitializeMatchWithThreePlayers()
        {
            // Arrange
            var mockCentralBoard = new CentralBoard();
            var testSession = new GameSession("TEST-001", mockCentralBoard, mockLoggerHelper.Object);
            mockSessionManager.Setup(x => x.GetSession("TEST-001")).Returns(testSession);

            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" },
            new GamePlayerInitDTO { UserId = 2, Nickname = "Player2" },
            new GamePlayerInitDTO { UserId = 3, Nickname = "Player3" }
        };

            // Act
            var result = await gameLogic.InitializeMatch("TEST-001", players);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestInitializeMatchWithFourPlayers()
        {
            // Arrange
            var mockCentralBoard = new CentralBoard();
            var testSession = new GameSession("TEST-001", mockCentralBoard, mockLoggerHelper.Object);
            mockSessionManager.Setup(x => x.GetSession("TEST-001")).Returns(testSession);

            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" },
            new GamePlayerInitDTO { UserId = 2, Nickname = "Player2" },
            new GamePlayerInitDTO { UserId = 3, Nickname = "Player3" },
            new GamePlayerInitDTO { UserId = 4, Nickname = "Player4" }
        };

            // Act
            var result = await gameLogic.InitializeMatch("TEST-001", players);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestInitializeMatchReturnsFalseWithNullMatchCode()
        {
            // Arrange
            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" },
            new GamePlayerInitDTO { UserId = 2, Nickname = "Player2" }
        };

            // Act
            var result = await gameLogic.InitializeMatch(null, players);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestInitializeMatchReturnsFalseWithEmptyMatchCode()
        {
            // Arrange
            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" },
            new GamePlayerInitDTO { UserId = 2, Nickname = "Player2" }
        };

            // Act
            var result = await gameLogic.InitializeMatch("", players);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestInitializeMatchReturnsFalseWithNullPlayers()
        {
            // Act
            var result = await gameLogic.InitializeMatch("TEST-001", null);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestInitializeMatchReturnsFalseWithLessThanTwoPlayers()
        {
            // Arrange
            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" }
        };

            // Act
            var result = await gameLogic.InitializeMatch("TEST-001", players);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestInitializeMatchReturnsFalseWithMoreThanFourPlayers()
        {
            // Arrange
            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" },
            new GamePlayerInitDTO { UserId = 2, Nickname = "Player2" },
            new GamePlayerInitDTO { UserId = 3, Nickname = "Player3" },
            new GamePlayerInitDTO { UserId = 4, Nickname = "Player4" },
            new GamePlayerInitDTO { UserId = 5, Nickname = "Player5" }
        };

            // Act
            var result = await gameLogic.InitializeMatch("TEST-001", players);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestInitializeMatchReturnsFalseWhenSessionAlreadyExists()
        {
            // Arrange
            mockSessionManager.Setup(x => x.SessionExists("TEST-001")).Returns(true);

            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" },
            new GamePlayerInitDTO { UserId = 2, Nickname = "Player2" }
        };

            // Act
            var result = await gameLogic.InitializeMatch("TEST-001", players);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestInitializeMatchReturnsFalseWhenCreateSessionFails()
        {
            // Arrange
            mockSessionManager.Setup(x => x.CreateSession("TEST-001")).Returns(false);

            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" },
            new GamePlayerInitDTO { UserId = 2, Nickname = "Player2" }
        };

            // Act
            var result = await gameLogic.InitializeMatch("TEST-001", players);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestInitializeMatchReturnsFalseWhenSetupFails()
        {
            // Arrange
            var mockCentralBoard = new CentralBoard();
            var testSession = new GameSession("TEST-001", mockCentralBoard, mockLoggerHelper.Object);
            mockSessionManager.Setup(x => x.GetSession("TEST-001")).Returns(testSession);
            mockSetupHandler.Setup(x => x.InitializeGameSession(It.IsAny<GameSession>(), It.IsAny<List<PlayerSession>>())).Returns(false);

            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" },
            new GamePlayerInitDTO { UserId = 2, Nickname = "Player2" }
        };

            // Act
            var result = await gameLogic.InitializeMatch("TEST-001", players);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestInitializeMatchSetsStartTime()
        {
            // Arrange
            var mockCentralBoard = new CentralBoard();
            var testSession = new GameSession("TEST-001", mockCentralBoard, mockLoggerHelper.Object);
            mockSessionManager.Setup(x => x.GetSession("TEST-001")).Returns(testSession);

            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" },
            new GamePlayerInitDTO { UserId = 2, Nickname = "Player2" }
        };

            // Act
            await gameLogic.InitializeMatch("TEST-001", players);

            // Assert
            Assert.IsNotNull(testSession.StartTime);
        }

        [TestMethod]
        public async Task TestInitializeMatchStartsFirstPlayerTurn()
        {
            // Arrange
            var mockCentralBoard = new CentralBoard();
            var testSession = new GameSession("TEST-001", mockCentralBoard, mockLoggerHelper.Object);
            mockSessionManager.Setup(x => x.GetSession("TEST-001")).Returns(testSession);

            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" },
            new GamePlayerInitDTO { UserId = 2, Nickname = "Player2" }
        };

            // Act
            await gameLogic.InitializeMatch("TEST-001", players);

            // Assert
            Assert.IsTrue(testSession.CurrentTurn > 0);
        }

        [TestMethod]
        public async Task TestInitializeMatchAssignsTurnOrderToPlayers()
        {
            // Arrange
            var mockCentralBoard = new CentralBoard();
            var testSession = new GameSession("TEST-001", mockCentralBoard, mockLoggerHelper.Object);
            mockSessionManager.Setup(x => x.GetSession("TEST-001")).Returns(testSession);

            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" },
            new GamePlayerInitDTO { UserId = 2, Nickname = "Player2" }
        };

            // Act
            await gameLogic.InitializeMatch("TEST-001", players);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyGameInitialized(
                It.Is<GameInitializedDTO>(dto =>
                    dto.Players.Any(p => p.TurnOrder > 0))),
                Times.Once);
        }

        [TestMethod]
        public async Task TestInitializeMatchLogsWarningWhenValidationFails()
        {
            // Arrange
            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" }
        };

            // Act
            await gameLogic.InitializeMatch("TEST-001", players);

            // Assert
            mockLoggerHelper.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task TestInitializeMatchDoesNotCreateSessionWhenValidationFails()
        {
            // Arrange
            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" }
        };

            // Act
            await gameLogic.InitializeMatch("TEST-001", players);

            // Assert
            mockSessionManager.Verify(x => x.CreateSession(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task TestInitializeMatchNotifiesArchsOnBoard()
        {
            // Arrange
            var mockCentralBoard = new CentralBoard();
            var testSession = new GameSession("TEST-001", mockCentralBoard, mockLoggerHelper.Object);
            mockSessionManager.Setup(x => x.GetSession("TEST-001")).Returns(testSession);

            var players = new List<GamePlayerInitDTO>
        {
            new GamePlayerInitDTO { UserId = 1, Nickname = "Player1" },
            new GamePlayerInitDTO { UserId = 2, Nickname = "Player2" }
        };

            // Act
            await gameLogic.InitializeMatch("TEST-001", players);

            // Assert
            mockGameNotifier.Verify(x => x.NotifyArchAddedToBoard(It.IsAny<ArchAddedToBoardDTO>()), Times.AtLeastOnce);
        }
    }
}
