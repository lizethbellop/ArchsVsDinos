using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Game;
using ArchsVsDinosServer.Interfaces.Lobby;
using ArchsVsDinosServer.Model;
using Contracts.DTO;
using Contracts.DTO.Response;
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
        private Mock<ILobbyValidationHelper> validation;
        private Mock<ILobbySession> session;
        private Mock<IGameLogic> gameLogic;
        private Mock<IInvitationSendHelper> invitationHelper;
        private LobbyLogic lobbyLogic;

        [TestInitialize]
        public void Setup()
        {
            BaseSetup();

            validation = new Mock<ILobbyValidationHelper>();
            session = new Mock<ILobbySession>();
            gameLogic = new Mock<IGameLogic>();
            invitationHelper = new Mock<IInvitationSendHelper>();

            var core = new LobbyCoreContext(
                session.Object,
                validation.Object,
                new Mock<ILobbyCodeGeneratorHelper>().Object
            );

            lobbyLogic = new LobbyLogic(
                core,
                mockLoggerHelper.Object,
                gameLogic.Object,
                invitationHelper.Object
            );
        }

        [TestMethod]
        public async Task TestJoinLobbyNullRequest()
        {
            var result = await lobbyLogic.JoinLobby(null);

            var expected = new MatchJoinResponse
            {
                Success = false,
                ResultCode = JoinMatchResultCode.JoinMatch_InvalidParameters
            };

            Assert.AreEqual(expected.ResultCode, result.ResultCode);
        }

        [TestMethod]
        public async Task TestJoinLobbyEmptyLobbyCode()
        {
            var request = new JoinLobbyRequest
            {
                LobbyCode = "",
                Nickname = "Player"
            };

            var result = await lobbyLogic.JoinLobby(request);

            Assert.AreEqual(
                JoinMatchResultCode.JoinMatch_InvalidParameters,
                result.ResultCode
            );
        }

        [TestMethod]
        public async Task TestJoinLobbyEmptyNickname()
        {
            var request = new JoinLobbyRequest
            {
                LobbyCode = "ABC12",
                Nickname = ""
            };

            var result = await lobbyLogic.JoinLobby(request);

            Assert.AreEqual(
                JoinMatchResultCode.JoinMatch_InvalidParameters,
                result.ResultCode
            );
        }

        [TestMethod]
        public async Task TestJoinLobbyLobbyNotFound()
        {
            var request = new JoinLobbyRequest
            {
                LobbyCode = "ABC12",
                Nickname = "Player"
            };

            session.Setup(s => s.GetLobby("ABC12"))
                   .Returns((ActiveLobbyData)null);

            var result = await lobbyLogic.JoinLobby(request);

            Assert.AreEqual(
                JoinMatchResultCode.JoinMatch_LobbyNotFound,
                result.ResultCode
            );
        }

        [TestMethod]
        public async Task TestJoinLobbyLobbyFull()
        {
            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 1,
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host"
            });

            lobby.AddPlayer(1, "host", "Host");

            session.Setup(s => s.GetLobby("ABC12"))
                   .Returns(lobby);

            var request = new JoinLobbyRequest
            {
                LobbyCode = "ABC12",
                Nickname = "Player"
            };

            var result = await lobbyLogic.JoinLobby(request);

            Assert.AreEqual(
                JoinMatchResultCode.JoinMatch_LobbyFull,
                result.ResultCode
            );
        }

        [TestMethod]
        public async Task TestJoinLobbySuccess()
        {
            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                MaxPlayers = 4,
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host"
            });

            session.Setup(s => s.GetLobby("ABC12"))
                   .Returns(lobby);

            var request = new JoinLobbyRequest
            {
                LobbyCode = "ABC12",
                UserId = 2,
                Username = "user",
                Nickname = "Player"
            };

            var result = await lobbyLogic.JoinLobby(request);

            var expected = new MatchJoinResponse
            {
                Success = true,
                ResultCode = JoinMatchResultCode.JoinMatch_Success,
                LobbyCode = "ABC12"
            };

            Assert.AreEqual(expected.ResultCode, result.ResultCode);
        }

        [TestMethod]
        public async Task TestJoinLobbyValidationThrowsArgumentException()
        {
            validation.Setup(v =>
                v.ValidateJoinLobby("ABC12", "Player"))
                .Throws(new ArgumentException());

            var request = new JoinLobbyRequest
            {
                LobbyCode = "ABC12",
                Nickname = "Player"
            };

            var result = await lobbyLogic.JoinLobby(request);

            Assert.AreEqual(
                JoinMatchResultCode.JoinMatch_InvalidSettings,
                result.ResultCode
            );
        }

        [TestMethod]
        public async Task TestJoinLobbyValidationThrowsTimeoutException()
        {
            validation.Setup(v =>
                v.ValidateJoinLobby("ABC12", "Player"))
                .Throws(new TimeoutException());

            var request = new JoinLobbyRequest
            {
                LobbyCode = "ABC12",
                Nickname = "Player"
            };

            var result = await lobbyLogic.JoinLobby(request);

            Assert.AreEqual(
                JoinMatchResultCode.JoinMatch_Timeout,
                result.ResultCode
            );
        }
    }


}
