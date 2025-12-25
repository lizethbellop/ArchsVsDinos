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
        public void TestKickPlayerSuccessful()
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

            mockSession.Verify(s => s.Broadcast("ABC12", It.IsAny<Action<ILobbyManagerCallback>>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void TestKickPlayerDisconnectsCallback()
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

            mockSession.Verify(s => s.DisconnectPlayerCallback("ABC12", "Player2"), Times.Once);
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
            mockSession.Setup(s => s.GetLobby("XXXXX")).Returns((ActiveLobbyData)null);

            lobbyLogic.KickPlayer("XXXXX", 100, "Player2");
        }

        [TestMethod]
        [ExpectedException(typeof(UnauthorizedAccessException))]
        public void TestKickPlayerNonHostCannotKick()
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
        public void TestKickPlayerCannotKickHost()
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
        public void TestKickPlayerOnlyHostValidated()
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

            bool exceptionThrown = false;

            try
            {
                lobbyLogic.KickPlayer("ABC12", 200, "Player2");
            }
            catch (UnauthorizedAccessException)
            {
                exceptionThrown = true;
            }

            Assert.IsTrue(exceptionThrown);
        }

        [TestMethod]
        [ExpectedException(typeof(CommunicationException))]
        public void TestKickPlayerHandlesCommunicationException()
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
            mockSession.Setup(s => s.Broadcast("ABC12", It.IsAny<Action<ILobbyManagerCallback>>()))
                .Throws(new CommunicationException("Network error"));

            lobbyLogic.KickPlayer("ABC12", 100, "Player2");
        }

        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public void TestKickPlayerHandlesTimeoutException()
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
            mockSession.Setup(s => s.Broadcast("ABC12", It.IsAny<Action<ILobbyManagerCallback>>()))
                .Throws(new TimeoutException("Request timeout"));

            lobbyLogic.KickPlayer("ABC12", 100, "Player2");
        }

        [TestMethod]
        public void TestKickPlayerRemovesPlayerFromList()
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

            var initialCount = lobby.Players.Count;
            lobbyLogic.KickPlayer("ABC12", 100, "Player2");
            var finalCount = lobby.Players.Count;

            Assert.AreEqual(initialCount - 1, finalCount);
        }

        [TestMethod]
        public void TestKickPlayerUpdatesPlayerList()
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
            mockSession.Setup(s => s.Broadcast("ABC12", It.IsAny<Action<ILobbyManagerCallback>>()))
                .Callback(() => broadcastCount++);

            lobbyLogic.KickPlayer("ABC12", 100, "Player2");

            Assert.IsTrue(broadcastCount >= 2);
        }

        [TestMethod]
        public void TestKickPlayerValidatesHostBeforePlayerExistence()
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

            bool correctExceptionThrown = false;

            try
            {
                lobbyLogic.KickPlayer("ABC12", 200, "NonExistentPlayer");
            }
            catch (UnauthorizedAccessException)
            {
                correctExceptionThrown = true;
            }
            catch (InvalidOperationException)
            {
                correctExceptionThrown = false;
            }

            Assert.IsTrue(correctExceptionThrown);
        }

        [TestMethod]
        public void TestKickPlayerCaseInsensitiveNickname()
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
