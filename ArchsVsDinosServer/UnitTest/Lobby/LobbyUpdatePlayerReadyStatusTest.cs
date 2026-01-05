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
    public class LobbyUpdatePlayerReadyStatusTest : BaseTestClass
    {
        private Mock<ILobbySession> mockSession;
        private Mock<ILobbyValidationHelper> mockValidation;
        private Mock<ILobbyCodeGeneratorHelper> mockCodeGenerator;
        private Mock<IGameLogic> mockGameLogic;
        private Mock<IInvitationSendHelper> mockInvitationHelper;

        private LobbyCoreContext coreContext;
        private LobbyLogic lobbyLogic;

        [TestInitialize]
        public void Setup()
        {
            BaseSetup();

            mockSession = new Mock<ILobbySession>();
            mockValidation = new Mock<ILobbyValidationHelper>();
            mockCodeGenerator = new Mock<ILobbyCodeGeneratorHelper>();
            mockGameLogic = new Mock<IGameLogic>();
            mockInvitationHelper = new Mock<IInvitationSendHelper>();

            coreContext = new LobbyCoreContext(
                mockSession.Object,
                mockValidation.Object,
                mockCodeGenerator.Object
            );

            lobbyLogic = new LobbyLogic(
                coreContext,
                mockLoggerHelper.Object,
                mockGameLogic.Object,
                mockInvitationHelper.Object
            );
        }

        private ActiveLobbyData CreateLobbyWithPlayers()
        {
            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host",
                MaxPlayers = 4
            });
            lobby.AddPlayer(1, "host", "Host");
            lobby.AddPlayer(2, "player2", "Player2");
            lobby.AddPlayer(3, "player3", "Player3");
            return lobby;
        }

        [TestMethod]
        public async Task TestUpdatePlayerReadyStatusEmptyLobbyCodeDoesNothing()
        {
            await lobbyLogic.UpdatePlayerReadyStatus("", "Player1", true);

            mockSession.Verify(
                s => s.GetLobby(It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public async Task TestUpdatePlayerReadyStatusNullLobbyCodeDoesNothing()
        {
            await lobbyLogic.UpdatePlayerReadyStatus(null, "Player1", true);

            mockSession.Verify(
                s => s.GetLobby(It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public async Task TestUpdatePlayerReadyStatusWhitespaceLobbyCodeDoesNothing()
        {
            await lobbyLogic.UpdatePlayerReadyStatus("   ", "Player1", true);

            mockSession.Verify(
                s => s.GetLobby(It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public async Task TestUpdatePlayerReadyStatusEmptyPlayerNameDoesNothing()
        {
            await lobbyLogic.UpdatePlayerReadyStatus("ABC12", "", true);

            mockSession.Verify(
                s => s.GetLobby(It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public async Task TestUpdatePlayerReadyStatusNullPlayerNameDoesNothing()
        {
            await lobbyLogic.UpdatePlayerReadyStatus("ABC12", null, true);

            mockSession.Verify(
                s => s.GetLobby(It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public async Task TestUpdatePlayerReadyStatusWhitespacePlayerNameDoesNothing()
        {
            await lobbyLogic.UpdatePlayerReadyStatus("ABC12", "   ", true);

            mockSession.Verify(
                s => s.GetLobby(It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public async Task TestUpdatePlayerReadyStatusLobbyNotFoundDoesNothing()
        {
            mockSession.Setup(s => s.GetLobby("WRONG"))
                       .Returns((ActiveLobbyData)null);

            await lobbyLogic.UpdatePlayerReadyStatus("WRONG", "Player1", true);

            mockSession.Verify(
                s => s.Broadcast(It.IsAny<string>(), It.IsAny<Action<ILobbyManagerCallback>>()),
                Times.Never);
        }

        [TestMethod]
        public async Task TestUpdatePlayerReadyStatusPlayerNotFoundDoesNotBroadcast()
        {
            var lobby = CreateLobbyWithPlayers();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            await lobbyLogic.UpdatePlayerReadyStatus("ABC12", "NonExistent", true);

            mockSession.Verify(
                s => s.Broadcast(It.IsAny<string>(), It.IsAny<Action<ILobbyManagerCallback>>()),
                Times.Never);
        }

        [TestMethod]
        public async Task TestUpdatePlayerReadyStatusSetsPlayerToReady()
        {
            var lobby = CreateLobbyWithPlayers();
            var player = lobby.Players.First(p => p.Nickname == "Player2");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            await lobbyLogic.UpdatePlayerReadyStatus("ABC12", "Player2", true);

            Assert.IsTrue(player.IsReady);
        }

        [TestMethod]
        public async Task TestUpdatePlayerReadyStatusSetsPlayerToNotReady()
        {
            var lobby = CreateLobbyWithPlayers();
            var player = lobby.Players.First(p => p.Nickname == "Player2");
            player.IsReady = true;

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            await lobbyLogic.UpdatePlayerReadyStatus("ABC12", "Player2", false);

            Assert.IsFalse(player.IsReady);
        }

        [TestMethod]
        public async Task TestUpdatePlayerReadyStatusBroadcastsStatusChange()
        {
            var lobby = CreateLobbyWithPlayers();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            await lobbyLogic.UpdatePlayerReadyStatus("ABC12", "Player2", true);

            mockSession.Verify(
                s => s.Broadcast("ABC12", It.IsAny<Action<ILobbyManagerCallback>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task TestUpdatePlayerReadyStatusBroadcastsCorrectPlayerName()
        {
            var lobby = CreateLobbyWithPlayers();
            string capturedPlayerName = null;
            bool capturedStatus = false;

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockSession.Setup(s => s.Broadcast("ABC12", It.IsAny<Action<ILobbyManagerCallback>>()))
                       .Callback<string, Action<ILobbyManagerCallback>>((code, action) =>
                       {
                           var mockCallback = new Mock<ILobbyManagerCallback>();
                           mockCallback.Setup(c => c.PlayerReadyStatusChanged(It.IsAny<string>(), It.IsAny<bool>()))
                                      .Callback<string, bool>((name, status) =>
                                      {
                                          capturedPlayerName = name;
                                          capturedStatus = status;
                                      });
                           action(mockCallback.Object);
                       });

            await lobbyLogic.UpdatePlayerReadyStatus("ABC12", "Player2", true);

            Assert.AreEqual("Player2", capturedPlayerName);
        }

        [TestMethod]
        public async Task TestUpdatePlayerReadyStatusBroadcastsCorrectStatus()
        {
            var lobby = CreateLobbyWithPlayers();
            bool capturedStatus = false;

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockSession.Setup(s => s.Broadcast("ABC12", It.IsAny<Action<ILobbyManagerCallback>>()))
                       .Callback<string, Action<ILobbyManagerCallback>>((code, action) =>
                       {
                           var mockCallback = new Mock<ILobbyManagerCallback>();
                           mockCallback.Setup(c => c.PlayerReadyStatusChanged(It.IsAny<string>(), It.IsAny<bool>()))
                                      .Callback<string, bool>((name, status) => capturedStatus = status);
                           action(mockCallback.Object);
                       });

            await lobbyLogic.UpdatePlayerReadyStatus("ABC12", "Player2", true);

            Assert.IsTrue(capturedStatus);
        }

        [TestMethod]
        public async Task TestUpdatePlayerReadyStatusCaseInsensitiveNickname()
        {
            var lobby = CreateLobbyWithPlayers();
            var player = lobby.Players.First(p => p.Nickname == "Player2");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            await lobbyLogic.UpdatePlayerReadyStatus("ABC12", "pLaYeR2", true);

            Assert.IsTrue(player.IsReady);
        }

        [TestMethod]
        public async Task TestUpdatePlayerReadyStatusWorksForHost()
        {
            var lobby = CreateLobbyWithPlayers();
            var host = lobby.Players.First(p => p.Nickname == "Host");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            await lobbyLogic.UpdatePlayerReadyStatus("ABC12", "Host", true);

            Assert.IsTrue(host.IsReady);
        }

        [TestMethod]
        public async Task TestUpdatePlayerReadyStatusToggleReadyMultipleTimes()
        {
            var lobby = CreateLobbyWithPlayers();
            var player = lobby.Players.First(p => p.Nickname == "Player2");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            await lobbyLogic.UpdatePlayerReadyStatus("ABC12", "Player2", true);
            Assert.IsTrue(player.IsReady);

            await lobbyLogic.UpdatePlayerReadyStatus("ABC12", "Player2", false);
            Assert.IsFalse(player.IsReady);

            await lobbyLogic.UpdatePlayerReadyStatus("ABC12", "Player2", true);
            Assert.IsTrue(player.IsReady);
        }

        [TestMethod]
        public async Task TestUpdatePlayerReadyStatusMultiplePlayersSeparately()
        {
            var lobby = CreateLobbyWithPlayers();
            var player2 = lobby.Players.First(p => p.Nickname == "Player2");
            var player3 = lobby.Players.First(p => p.Nickname == "Player3");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            await lobbyLogic.UpdatePlayerReadyStatus("ABC12", "Player2", true);
            await lobbyLogic.UpdatePlayerReadyStatus("ABC12", "Player3", true);

            Assert.IsTrue(player2.IsReady);
            Assert.IsTrue(player3.IsReady);
        }

        [TestMethod]
        public async Task TestUpdatePlayerReadyStatusBroadcastsEachTimeIndependently()
        {
            var lobby = CreateLobbyWithPlayers();
            int broadcastCount = 0;

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockSession.Setup(s => s.Broadcast(It.IsAny<string>(), It.IsAny<Action<ILobbyManagerCallback>>()))
                       .Callback(() => broadcastCount++);

            await lobbyLogic.UpdatePlayerReadyStatus("ABC12", "Player2", true);
            await lobbyLogic.UpdatePlayerReadyStatus("ABC12", "Player3", true);

            Assert.AreEqual(2, broadcastCount);
        }

        [TestMethod]
        public async Task TestUpdatePlayerReadyStatusDoesNotChangeOtherPlayers()
        {
            var lobby = CreateLobbyWithPlayers();
            var player2 = lobby.Players.First(p => p.Nickname == "Player2");
            var player3 = lobby.Players.First(p => p.Nickname == "Player3");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            await lobbyLogic.UpdatePlayerReadyStatus("ABC12", "Player2", true);

            Assert.IsTrue(player2.IsReady);
            Assert.IsFalse(player3.IsReady);
        }
    }
}
