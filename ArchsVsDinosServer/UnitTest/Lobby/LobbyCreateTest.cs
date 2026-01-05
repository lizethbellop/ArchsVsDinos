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
    public class LobbyCreateTest : BaseTestClass
    {
        private Mock<ILobbySession> mockSession;
        private Mock<ILobbyValidationHelper> mockValidation;
        private Mock<ILobbyCodeGeneratorHelper> mockCodeGenerator;
        private Mock<IGameLogic> mockGameLogic;
        private Mock<IInvitationSendHelper> mockInvitationHelper;

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

            var coreContext = new LobbyCoreContext(
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

        private MatchSettings ValidSettings() => new MatchSettings
        {
            HostUserId = 1,
            HostUsername = "host",
            HostNickname = "Host",
            MaxPlayers = 4
        };

        [TestMethod]
        public async Task TestCreateLobbyInvalidParameters()
        {
            MatchCreationResponse result = await lobbyLogic.CreateLobby(null);

            Assert.AreEqual(
                new MatchCreationResponse
                {
                    Success = false,
                    ResultCode = MatchCreationResultCode.MatchCreation_InvalidParameters
                },
                result);
        }

        [TestMethod]
        public async Task TestCreateLobbyInvalidSettings()
        {
            var settings = ValidSettings();

            mockValidation
                .Setup(v => v.ValidateCreateLobby(settings))
                .Throws(new ArgumentException());

            MatchCreationResponse result = await lobbyLogic.CreateLobby(settings);

            Assert.AreEqual(
                new MatchCreationResponse
                {
                    Success = false,
                    ResultCode = MatchCreationResultCode.MatchCreation_InvalidSettings
                },
                result);
        }

        [TestMethod]
        public async Task TestCreateLobbyServerBusy()
        {
            var settings = ValidSettings();

            mockCodeGenerator
                .Setup(g => g.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Throws(new InvalidOperationException());

            MatchCreationResponse result = await lobbyLogic.CreateLobby(settings);

            Assert.AreEqual(
                new MatchCreationResponse
                {
                    Success = false,
                    ResultCode = MatchCreationResultCode.MatchCreation_ServerBusy
                },
                result);
        }

        [TestMethod]
        public async Task TestCreateLobbyTimeout()
        {
            var settings = ValidSettings();

            mockCodeGenerator
                .Setup(g => g.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Throws(new TimeoutException());

            MatchCreationResponse result = await lobbyLogic.CreateLobby(settings);

            Assert.AreEqual(
                new MatchCreationResponse
                {
                    Success = false,
                    ResultCode = MatchCreationResultCode.MatchCreation_Timeout
                },
                result);
        }

        [TestMethod]
        public async Task TestCreateLobbySuccess()
        {
            var settings = ValidSettings();

            mockCodeGenerator
                .Setup(g => g.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Returns("ABCDE");

            MatchCreationResponse result = await lobbyLogic.CreateLobby(settings);

            Assert.AreEqual(
                new MatchCreationResponse
                {
                    Success = true,
                    LobbyCode = "ABCDE",
                    ResultCode = MatchCreationResultCode.MatchCreation_Success
                },
                result);
        }

        [TestMethod]
        public async Task TestCreateLobbyCallsValidate()
        {
            var settings = ValidSettings();

            mockCodeGenerator
                .Setup(g => g.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Returns("ABCDE");

            await lobbyLogic.CreateLobby(settings);

            mockValidation.Verify(v => v.ValidateCreateLobby(settings), Times.Once);
        }

        [TestMethod]
        public async Task TestCreateLobbyCallsGenerateCode()
        {
            var settings = ValidSettings();

            mockCodeGenerator
                .Setup(g => g.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Returns("ABCDE");

            await lobbyLogic.CreateLobby(settings);

            mockCodeGenerator.Verify(
                g => g.GenerateLobbyCode(It.IsAny<Func<string, bool>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task TestCreateLobbyCallsCreateLobby()
        {
            var settings = ValidSettings();

            mockCodeGenerator
                .Setup(g => g.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Returns("ABCDE");

            await lobbyLogic.CreateLobby(settings);

            mockSession.Verify(
                s => s.CreateLobby("ABCDE", It.IsAny<ActiveLobbyData>()),
                Times.Once);
        }
    }


}
