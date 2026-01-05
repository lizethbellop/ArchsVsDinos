using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.GameManagement;
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
using System.Threading.Tasks;

namespace UnitTest.Game
{
    [TestClass]
    public class GameInitializeMatchTest : BaseTestClass
    {
        private GameSessionManager sessionManager;
        private GameSetupHandler setupHandler;
        private Mock<IGameNotifier> mockGameNotifier;
        private Mock<IStatisticsManager> mockStatisticsManager;
        private GameCoreContext gameCoreContext;
        private GameLogic gameLogic;

        [TestInitialize]
        public void SetupInitializeMatchTests()
        {
            base.BaseSetup();

            sessionManager = new GameSessionManager(mockLoggerHelper.Object);
            setupHandler = new GameSetupHandler();
            mockGameNotifier = new Mock<IGameNotifier>();
            mockStatisticsManager = new Mock<IStatisticsManager>();

            gameCoreContext = new GameCoreContext(sessionManager, setupHandler);

            var dependencies = new GameLogicDependencies(
                gameCoreContext,
                mockLoggerHelper.Object,
                mockGameNotifier.Object,
                mockStatisticsManager.Object
            );

            gameLogic = new GameLogic(dependencies);
        }

        [TestMethod]
        public async Task TestInitializeMatch_Success_MinimumPlayers()
        {
            // Arrange
            var players = CreatePlayerList(2);

            // Act
            var result = await gameLogic.InitializeMatch("MATCH-01", players);

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(sessionManager.SessionExists("MATCH-01"));
            mockGameNotifier.Verify(n => n.NotifyGameInitialized(It.IsAny<GameInitializedDTO>()), Times.Once);
        }

        [TestMethod]
        public async Task TestInitializeMatch_Success_MaximumPlayers()
        {
            // Arrange
            var players = CreatePlayerList(4);

            // Act
            var result = await gameLogic.InitializeMatch("MATCH-02", players);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(4, sessionManager.GetSession("MATCH-02").Players.Count);
        }

        [TestMethod]
        public async Task TestInitializeMatch_StartsFirstTurnCorrectly()
        {
            // Arrange
            var players = CreatePlayerList(2);

            // Act
            await gameLogic.InitializeMatch("MATCH-03", players);
            var session = sessionManager.GetSession("MATCH-03");

            // Assert
            Assert.IsTrue(session.CurrentTurn > 0);
            Assert.IsNotNull(session.StartTime);
        }

        [TestMethod]
        public async Task TestInitializeMatch_Fails_WhenMatchCodeExists()
        {
            // Arrange
            sessionManager.CreateSession("DUPLICATE");
            var players = CreatePlayerList(2);

            // Act
            var result = await gameLogic.InitializeMatch("DUPLICATE", players);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestInitializeMatch_Fails_WhenTooFewPlayers()
        {
            // Arrange
            var players = CreatePlayerList(1);

            // Act
            var result = await gameLogic.InitializeMatch("FAIL-1", players);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestInitializeMatch_Fails_WhenTooManyPlayers()
        {
            // Arrange
            var players = CreatePlayerList(5);

            // Act
            var result = await gameLogic.InitializeMatch("FAIL-5", players);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestInitializeMatch_NotificationsSentToAllPlayers()
        {
            // Arrange
            var players = CreatePlayerList(3);

            // Act
            await gameLogic.InitializeMatch("NOTIFY-3", players);

            // Assert
            mockGameNotifier.Verify(n => n.NotifyGameStarted(It.IsAny<GameStartedDTO>()), Times.Exactly(3));
        }


        [TestMethod]
        public async Task TestInitializeMatch_NotifiesInitialArchs()
        {
            // Arrange
            var players = CreatePlayerList(2);

            // Act
            await gameLogic.InitializeMatch("ARCH-TEST", players);

            // Assert
            mockGameNotifier.Verify(n => n.NotifyArchAddedToBoard(It.Is<ArchAddedToBoardDTO>(d =>
                d.PlayerUsername == "System")),
                Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task TestInitializeMatch_DeckIsCreatedAndFilled()
        {
            // Arrange
            var players = CreatePlayerList(2);

            // Act
            await gameLogic.InitializeMatch("DECK-TEST", players);
            var session = sessionManager.GetSession("DECK-TEST");

            // Assert
            Assert.IsTrue(session.DrawDeck.Count > 0);
        }

        [TestMethod]
        public async Task TestInitializeMatch_PlayersGetInitialHands()
        {
            // Arrange
            var players = CreatePlayerList(2);

            // Act
            await gameLogic.InitializeMatch("HAND-TEST", players);
            var session = sessionManager.GetSession("HAND-TEST");

            // Assert
            foreach (var p in session.Players)
            {
                Assert.IsTrue(p.Hand.Count > 0);
            }
        }

        [TestMethod]
        public async Task TestInitializeMatch_CleansUp_WhenInitializationFails()
        {
            // Arrange
            var players = new List<GamePlayerInitDTO>();

            // Act
            var result = await gameLogic.InitializeMatch("CLEANUP-TEST", players);

            // Assert
            Assert.IsFalse(result);
            Assert.IsFalse(sessionManager.SessionExists("CLEANUP-TEST"));
        }

        private List<GamePlayerInitDTO> CreatePlayerList(int count)
        {
            return Enumerable.Range(1, count)
                .Select(i => new GamePlayerInitDTO
                {
                    UserId = i,
                    Nickname = $"Player{i}"
                })
                .ToList();
        }
    }
}