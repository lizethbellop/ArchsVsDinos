using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Game;
using ArchsVsDinosServer.Interfaces.Lobby;
using ArchsVsDinosServer.Model;
using Contracts.DTO;
using Contracts.DTO.Result_Codes;
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
    public class LobbyJoinTest : BaseTestClass
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
        public async Task TestLobbyJoinSuccessful()
        {
            var request = new JoinLobbyRequest
            {
                LobbyCode = "ABC12",
                UserId = 100,
                Username = "testuser",
                Nickname = "TestPlayer"
            };

            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 4,
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host"
            });

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            var result = await lobbyLogic.JoinLobby(request);

            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task TestLobbyJoinReturnsSuccessCode()
        {
            var request = new JoinLobbyRequest
            {
                LobbyCode = "ABC12",
                UserId = 100,
                Username = "testuser",
                Nickname = "TestPlayer"
            };

            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 4,
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host"
            });

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            var result = await lobbyLogic.JoinLobby(request);

            Assert.AreEqual(JoinMatchResultCode.JoinMatch_Success, result.ResultCode);
        }

        [TestMethod]
        public async Task TestLobbyJoinReturnsCorrectLobbyCode()
        {
            var request = new JoinLobbyRequest
            {
                LobbyCode = "ABC12",
                UserId = 100,
                Username = "testuser",
                Nickname = "TestPlayer"
            };

            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 4,
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host"
            });

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            var result = await lobbyLogic.JoinLobby(request);

            Assert.AreEqual("ABC12", result.LobbyCode);
        }

        [TestMethod]
        public async Task TestLobbyJoinNullRequest()
        {
            var result = await lobbyLogic.JoinLobby(null);

            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public async Task TestLobbyJoinNullRequestReturnsInvalidParametersCode()
        {
            var result = await lobbyLogic.JoinLobby(null);

            Assert.AreEqual(JoinMatchResultCode.JoinMatch_InvalidParameters, result.ResultCode);
        }

        [TestMethod]
        public async Task TestLobbyJoinEmptyLobbyCode()
        {
            var request = new JoinLobbyRequest
            {
                LobbyCode = "",
                UserId = 100,
                Username = "testuser",
                Nickname = "TestPlayer"
            };

            var result = await lobbyLogic.JoinLobby(request);

            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public async Task TestLobbyJoinEmptyNickname()
        {
            var request = new JoinLobbyRequest
            {
                LobbyCode = "ABC12",
                UserId = 100,
                Username = "testuser",
                Nickname = ""
            };

            var result = await lobbyLogic.JoinLobby(request);

            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public async Task TestLobbyJoinLobbyNotFound()
        {
            var request = new JoinLobbyRequest
            {
                LobbyCode = "XXXXX",
                UserId = 100,
                Username = "testuser",
                Nickname = "TestPlayer"
            };

            mockSession.Setup(s => s.GetLobby("XXXXX")).Returns((ActiveLobbyData)null);

            var result = await lobbyLogic.JoinLobby(request);

            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public async Task TestLobbyJoinLobbyNotFoundReturnsCorrectCode()
        {
            var request = new JoinLobbyRequest
            {
                LobbyCode = "XXXXX",
                UserId = 100,
                Username = "testuser",
                Nickname = "TestPlayer"
            };

            mockSession.Setup(s => s.GetLobby("XXXXX")).Returns((ActiveLobbyData)null);

            var result = await lobbyLogic.JoinLobby(request);

            Assert.AreEqual(JoinMatchResultCode.JoinMatch_LobbyNotFound, result.ResultCode);
        }

        [TestMethod]
        public async Task TestLobbyJoinLobbyFull()
        {
            var request = new JoinLobbyRequest
            {
                LobbyCode = "ABC12",
                UserId = 100,
                Username = "testuser",
                Nickname = "TestPlayer"
            };

            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 2,
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host"
            });

            lobby.AddPlayer(1, "host", "Host");
            lobby.AddPlayer(2, "player2", "Player2");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            var result = await lobbyLogic.JoinLobby(request);

            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public async Task TestLobbyJoinLobbyFullReturnsCorrectCode()
        {
            var request = new JoinLobbyRequest
            {
                LobbyCode = "ABC12",
                UserId = 100,
                Username = "testuser",
                Nickname = "TestPlayer"
            };

            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 2,
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host"
            });

            lobby.AddPlayer(1, "host", "Host");
            lobby.AddPlayer(2, "player2", "Player2");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            var result = await lobbyLogic.JoinLobby(request);

            Assert.AreEqual(JoinMatchResultCode.JoinMatch_LobbyFull, result.ResultCode);
        }

        [TestMethod]
        public async Task TestLobbyJoinGuestPlayerWithZeroUserId()
        {
            var request = new JoinLobbyRequest
            {
                LobbyCode = "ABC12",
                UserId = 0,
                Username = null,
                Nickname = "GuestPlayer"
            };

            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 4,
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host"
            });

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            var result = await lobbyLogic.JoinLobby(request);

            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task TestLobbyJoinGuestPlayerAssignsNegativeUserId()
        {
            var request = new JoinLobbyRequest
            {
                LobbyCode = "ABC12",
                UserId = 0,
                Username = null,
                Nickname = "GuestPlayer"
            };

            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 4,
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host"
            });

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            await lobbyLogic.JoinLobby(request);

            var addedPlayer = lobby.Players.FirstOrDefault(p => p.Nickname == "GuestPlayer");
            Assert.IsNotNull(addedPlayer);
            Assert.IsTrue(addedPlayer.UserId < 0);
        }

        [TestMethod]
        public async Task TestLobbyJoinArgumentExceptionHandling()
        {
            var request = new JoinLobbyRequest
            {
                LobbyCode = "ABC12",
                UserId = 100,
                Username = "testuser",
                Nickname = "TestPlayer"
            };

            mockValidationHelper
                .Setup(v => v.ValidateJoinLobby("ABC12", "TestPlayer"))
                .Throws(new ArgumentException("Invalid parameters"));

            var result = await lobbyLogic.JoinLobby(request);

            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public async Task TestLobbyJoinArgumentExceptionReturnsInvalidSettingsCode()
        {
            var request = new JoinLobbyRequest
            {
                LobbyCode = "ABC12",
                UserId = 100,
                Username = "testuser",
                Nickname = "TestPlayer"
            };

            mockValidationHelper
                .Setup(v => v.ValidateJoinLobby("ABC12", "TestPlayer"))
                .Throws(new ArgumentException("Invalid parameters"));

            var result = await lobbyLogic.JoinLobby(request);

            Assert.AreEqual(JoinMatchResultCode.JoinMatch_InvalidSettings, result.ResultCode);
        }

        [TestMethod]
        public async Task TestLobbyJoinTimeoutExceptionHandling()
        {
            var request = new JoinLobbyRequest
            {
                LobbyCode = "ABC12",
                UserId = 100,
                Username = "testuser",
                Nickname = "TestPlayer"
            };

            mockValidationHelper
                .Setup(v => v.ValidateJoinLobby("ABC12", "TestPlayer"))
                .Throws(new TimeoutException("Request timeout"));

            var result = await lobbyLogic.JoinLobby(request);

            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public async Task TestLobbyJoinTimeoutExceptionReturnsTimeoutCode()
        {
            var request = new JoinLobbyRequest
            {
                LobbyCode = "ABC12",
                UserId = 100,
                Username = "testuser",
                Nickname = "TestPlayer"
            };

            mockValidationHelper
                .Setup(v => v.ValidateJoinLobby("ABC12", "TestPlayer"))
                .Throws(new TimeoutException("Request timeout"));

            var result = await lobbyLogic.JoinLobby(request);

            Assert.AreEqual(JoinMatchResultCode.JoinMatch_Timeout, result.ResultCode);
        }

        [TestMethod]
        public async Task TestLobbyJoinValidationIsCalled()
        {
            var request = new JoinLobbyRequest
            {
                LobbyCode = "ABC12",
                UserId = 100,
                Username = "testuser",
                Nickname = "TestPlayer"
            };

            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 4,
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host"
            });

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            await lobbyLogic.JoinLobby(request);

            mockValidationHelper.Verify(v => v.ValidateJoinLobby("ABC12", "TestPlayer"), Times.Once);
        }
    }
}
