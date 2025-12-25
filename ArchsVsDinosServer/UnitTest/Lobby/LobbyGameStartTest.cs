using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Game;
using ArchsVsDinosServer.Interfaces.Lobby;
using ArchsVsDinosServer.Model;
using Contracts;
using Contracts.DTO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.Lobby
{
    [TestClass]
    public class LobbyGameStartTest : BaseTestClass
    {
        private Mock<ILobbyValidationHelper> mockValidationHelper;
        private Mock<ILobbyCodeGeneratorHelper> mockCodeGenerator;
        private Mock<ILobbySession> mockSession;
        private Mock<IGameLogic> mockGameLogic;
        private Mock<IInvitationSendHelper> mockInvitationHelper;
        private Mock<ILobbyManagerCallback> mockCallback;
        private LobbyCoreContext coreContext;
        private LobbyLogic lobbyLogic;

        [TestInitialize]
        public void Setup()
        {
            BaseSetup();

            mockValidationHelper = new Mock<ILobbyValidationHelper>();
            mockCodeGenerator = new Mock<ILobbyCodeGeneratorHelper>();
            mockSession = new Mock<ILobbySession>();
            mockGameLogic = new Mock<IGameLogic>();
            mockInvitationHelper = new Mock<IInvitationSendHelper>();
            mockCallback = new Mock<ILobbyManagerCallback>();

            coreContext = new LobbyCoreContext(
                mockSession.Object,
                mockValidationHelper.Object,
                mockCodeGenerator.Object
            );

            lobbyLogic = new LobbyLogic(
                coreContext,
                mockLoggerHelper.Object,
                mockGameLogic.Object,
                mockInvitationHelper.Object
            );
        }

        [TestMethod]
        public async Task TestGameStartWithHostUser()
        {
            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 4,
                HostUserId = 100,
                HostUsername = "host",
                HostNickname = "Host"
            });

            lobby.AddPlayer(100, "host", "Host");
            lobby.AddPlayer(200, "player2", "Player2");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockGameLogic.Setup(g => g.InitializeMatch("ABC12", It.IsAny<List<GamePlayerInitDTO>>()))
                .ReturnsAsync(true);

            await lobbyLogic.EvaluateGameStart("ABC12", 100);

            mockGameLogic.Verify(g => g.InitializeMatch("ABC12", It.IsAny<List<GamePlayerInitDTO>>()), Times.Once);
        }

        [TestMethod]
        public async Task TestGameStartNonHostUserIsRejected()
        {
            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 4,
                HostUserId = 100,
                HostUsername = "host",
                HostNickname = "Host"
            });

            lobby.AddPlayer(100, "host", "Host");
            lobby.AddPlayer(200, "player2", "Player2");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            await lobbyLogic.EvaluateGameStart("ABC12", 200);

            mockGameLogic.Verify(g => g.InitializeMatch(It.IsAny<string>(), It.IsAny<List<GamePlayerInitDTO>>()), Times.Never);
        }

        [TestMethod]
        public async Task TestGameStartWithMinimumPlayers()
        {
            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 4,
                HostUserId = 100,
                HostUsername = "host",
                HostNickname = "Host"
            });

            lobby.AddPlayer(100, "host", "Host");
            lobby.AddPlayer(200, "player2", "Player2");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockGameLogic.Setup(g => g.InitializeMatch("ABC12", It.IsAny<List<GamePlayerInitDTO>>()))
                .ReturnsAsync(true);

            await lobbyLogic.EvaluateGameStart("ABC12", 100);

            mockGameLogic.Verify(g => g.InitializeMatch("ABC12", It.IsAny<List<GamePlayerInitDTO>>()), Times.Once);
        }

        [TestMethod]
        public async Task TestGameStartWithInsufficientPlayers()
        {
            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 4,
                HostUserId = 100,
                HostUsername = "host",
                HostNickname = "Host"
            });

            lobby.AddPlayer(100, "host", "Host");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            await lobbyLogic.EvaluateGameStart("ABC12", 100);

            mockGameLogic.Verify(g => g.InitializeMatch(It.IsAny<string>(), It.IsAny<List<GamePlayerInitDTO>>()), Times.Never);
        }

        [TestMethod]
        public async Task TestGameStartLobbyNotFound()
        {
            mockSession.Setup(s => s.GetLobby("XXXXX")).Returns((ActiveLobbyData)null);

            await lobbyLogic.EvaluateGameStart("XXXXX", 100);

            mockGameLogic.Verify(g => g.InitializeMatch(It.IsAny<string>(), It.IsAny<List<GamePlayerInitDTO>>()), Times.Never);
        }

        [TestMethod]
        public async Task TestGameStartInitializeMatchIsCalled()
        {
            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 4,
                HostUserId = 100,
                HostUsername = "host",
                HostNickname = "Host"
            });

            lobby.AddPlayer(100, "host", "Host");
            lobby.AddPlayer(200, "player2", "Player2");
            lobby.AddPlayer(300, "player3", "Player3");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockGameLogic.Setup(g => g.InitializeMatch("ABC12", It.IsAny<List<GamePlayerInitDTO>>()))
                .ReturnsAsync(true);

            await lobbyLogic.EvaluateGameStart("ABC12", 100);

            await Task.Delay(2500);

            mockGameLogic.Verify(g => g.InitializeMatch("ABC12",
                It.Is<List<GamePlayerInitDTO>>(list => list.Count == 3)), Times.Once);
        }

        [TestMethod]
        public async Task TestGameStartBroadcastGameStartingIsCalled()
        {
            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 4,
                HostUserId = 100,
                HostUsername = "host",
                HostNickname = "Host"
            });

            lobby.AddPlayer(100, "host", "Host");
            lobby.AddPlayer(200, "player2", "Player2");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockGameLogic.Setup(g => g.InitializeMatch("ABC12", It.IsAny<List<GamePlayerInitDTO>>()))
                .ReturnsAsync(true);

            await lobbyLogic.EvaluateGameStart("ABC12", 100);

            await Task.Delay(2500);

            mockSession.Verify(s => s.Broadcast("ABC12", It.IsAny<Action<ILobbyManagerCallback>>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task TestGameStartInitializeMatchFailsDoesNotBroadcast()
        {
            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 4,
                HostUserId = 100,
                HostUsername = "host",
                HostNickname = "Host"
            });

            lobby.AddPlayer(100, "host", "Host");
            lobby.AddPlayer(200, "player2", "Player2");

            int broadcastCount = 0;

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockGameLogic.Setup(g => g.InitializeMatch("ABC12", It.IsAny<List<GamePlayerInitDTO>>()))
                .ReturnsAsync(false);

            mockSession.Setup(s => s.Broadcast("ABC12", It.IsAny<Action<ILobbyManagerCallback>>()))
                .Callback(() => broadcastCount++);

            await lobbyLogic.EvaluateGameStart("ABC12", 100);

            await Task.Delay(2500);

            Assert.AreEqual(0, broadcastCount);
        }

        [TestMethod]
        public async Task TestGameStartWithCorrectPlayerData()
        {
            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 4,
                HostUserId = 100,
                HostUsername = "host",
                HostNickname = "Host"
            });

            lobby.AddPlayer(100, "host", "Host");
            lobby.AddPlayer(200, "player2", "Player2");

            List<GamePlayerInitDTO> capturedPlayers = null;

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockGameLogic.Setup(g => g.InitializeMatch("ABC12", It.IsAny<List<GamePlayerInitDTO>>()))
                .Callback<string, List<GamePlayerInitDTO>>((code, players) => capturedPlayers = players)
                .ReturnsAsync(true);

            await lobbyLogic.EvaluateGameStart("ABC12", 100);

            await Task.Delay(2500);

            Assert.IsNotNull(capturedPlayers);
            Assert.AreEqual(2, capturedPlayers.Count);
            Assert.IsTrue(capturedPlayers.Any(p => p.UserId == 100 && p.Nickname == "Host"));
            Assert.IsTrue(capturedPlayers.Any(p => p.UserId == 200 && p.Nickname == "Player2"));
        }

        [TestMethod]
        public async Task TestGameStartValidatesHostUserId()
        {
            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 4,
                HostUserId = 100,
                HostUsername = "host",
                HostNickname = "Host"
            });

            lobby.AddPlayer(100, "host", "Host");
            lobby.AddPlayer(200, "player2", "Player2");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            await lobbyLogic.EvaluateGameStart("ABC12", 999);

            mockGameLogic.Verify(g => g.InitializeMatch(It.IsAny<string>(), It.IsAny<List<GamePlayerInitDTO>>()), Times.Never);
        }

        [TestMethod]
        public async Task TestGameStartWaitsBeforeInitializing()
        {
            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 4,
                HostUserId = 100,
                HostUsername = "host",
                HostNickname = "Host"
            });

            lobby.AddPlayer(100, "host", "Host");
            lobby.AddPlayer(200, "player2", "Player2");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockGameLogic.Setup(g => g.InitializeMatch("ABC12", It.IsAny<List<GamePlayerInitDTO>>()))
                .ReturnsAsync(true);

            var startTime = DateTime.UtcNow;
            await lobbyLogic.EvaluateGameStart("ABC12", 100);
            await Task.Delay(2500);
            var endTime = DateTime.UtcNow;

            var elapsed = (endTime - startTime).TotalMilliseconds;
            Assert.IsTrue(elapsed >= 2000);
        }

        [TestMethod]
        public async Task TestGameStartLobbyDisappearsAfterDelay()
        {
            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 4,
                HostUserId = 100,
                HostUsername = "host",
                HostNickname = "Host"
            });

            lobby.AddPlayer(100, "host", "Host");
            lobby.AddPlayer(200, "player2", "Player2");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            await lobbyLogic.EvaluateGameStart("ABC12", 100);

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns((ActiveLobbyData)null);

            await Task.Delay(2500);

            mockGameLogic.Verify(g => g.InitializeMatch(It.IsAny<string>(), It.IsAny<List<GamePlayerInitDTO>>()), Times.Never);
        }

        [TestMethod]
        public async Task TestGameStartPlayerCountDropsBelowMinimum()
        {
            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 4,
                HostUserId = 100,
                HostUsername = "host",
                HostNickname = "Host"
            });

            lobby.AddPlayer(100, "host", "Host");
            lobby.AddPlayer(200, "player2", "Player2");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            await lobbyLogic.EvaluateGameStart("ABC12", 100);

            lobby.RemovePlayer("Player2");

            await Task.Delay(2500);

            mockGameLogic.Verify(g => g.InitializeMatch(It.IsAny<string>(), It.IsAny<List<GamePlayerInitDTO>>()), Times.Never);
        }
    }
}
