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
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.Lobby
{
    [TestClass]
    public class LobbyKickTest : BaseTestClass
    {
        private Mock<ILobbyValidationHelper> mockValidationHelper;
        private Mock<ILobbyCodeGeneratorHelper> mockCodeGenerator;
        private Mock<ILobbySession> mockSession;
        private Mock<IGameLogic> mockGameLogic;
        private Mock<IInvitationSendHelper> mockInvitationHelper;
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
        public void TestKickPlayerRemovesTargetPlayer()
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

            lobbyLogic.KickPlayer("ABC12", 100, "Player2");

            var remainingPlayer = lobby.Players.FirstOrDefault(p => p.Nickname == "Player2");
            Assert.IsNull(remainingPlayer);
        }

        [TestMethod]
        public void TestKickPlayerBroadcastsKickMessage()
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

            lobbyLogic.KickPlayer("ABC12", 100, "Player2");

            mockSession.Verify(
                s => s.Broadcast("ABC12", It.IsAny<Action<ILobbyManagerCallback>>()),
                Times.AtLeastOnce
            );
        }

        [TestMethod]
        public void TestKickPlayerDisconnectsTargetCallback()
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

            lobbyLogic.KickPlayer("ABC12", 100, "Player2");

            mockSession.Verify(
                s => s.DisconnectPlayerCallback("ABC12", "Player2"),
                Times.Once
            );
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestKickPlayerEmptyLobbyCodeThrowsException()
        {
            lobbyLogic.KickPlayer("", 100, "Player2");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestKickPlayerEmptyTargetNicknameThrowsException()
        {
            lobbyLogic.KickPlayer("ABC12", 100, "");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestKickPlayerLobbyNotFoundThrowsException()
        {
            mockSession.Setup(s => s.GetLobby("XXXXX"))
                       .Returns((ActiveLobbyData)null);

            lobbyLogic.KickPlayer("XXXXX", 100, "Player2");
        }

        [TestMethod]
        [ExpectedException(typeof(UnauthorizedAccessException))]
        public void TestKickPlayerNonHostThrowsException()
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

            lobbyLogic.KickPlayer("ABC12", 200, "Player3");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestKickPlayerTargetNotFoundThrowsException()
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

            lobbyLogic.KickPlayer("ABC12", 100, "NonExistentPlayer");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestKickPlayerCannotKickHostThrowsException()
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

            lobbyLogic.KickPlayer("ABC12", 100, "Host");
        }

        [TestMethod]
        public void TestKickPlayerValidatesOnlyHostCanKick()
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

            Assert.ThrowsException<UnauthorizedAccessException>(() =>
                lobbyLogic.KickPlayer("ABC12", 200, "Player2")
            );
        }

        [TestMethod]
        [ExpectedException(typeof(CommunicationException))]
        public void TestKickPlayerBroadcastCommunicationExceptionPropagates()
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
            mockSession.Setup(s =>
                s.Broadcast("ABC12", It.IsAny<Action<ILobbyManagerCallback>>()))
                .Throws(new CommunicationException());

            lobbyLogic.KickPlayer("ABC12", 100, "Player2");
        }

        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public void TestKickPlayerBroadcastTimeoutExceptionPropagates()
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
            mockSession.Setup(s =>
                s.Broadcast("ABC12", It.IsAny<Action<ILobbyManagerCallback>>()))
                .Throws(new TimeoutException());

            lobbyLogic.KickPlayer("ABC12", 100, "Player2");
        }

        [TestMethod]
        public void TestKickPlayerBroadcastsUpdatedPlayerList()
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
            mockSession.Setup(s =>
                s.Broadcast("ABC12", It.IsAny<Action<ILobbyManagerCallback>>()))
                .Callback(() => broadcastCount++);

            lobbyLogic.KickPlayer("ABC12", 100, "Player2");

            Assert.IsTrue(broadcastCount >= 2);
        }

        [TestMethod]
        public void TestKickPlayerIgnoresNicknameCase()
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

            lobbyLogic.KickPlayer("ABC12", 100, "pLayEr2");

            var remainingPlayer = lobby.Players.FirstOrDefault(p => p.Nickname == "Player2");
            Assert.IsNull(remainingPlayer);
        }
    }

}
