using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Game;
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
    public class LobbyCreateTest : BaseTestClass
    {
        private Mock<LobbySession> mockSession;
        private Mock<LobbyValidationHelper> mockValidation;
        private Mock<LobbyCodeGeneratorHelper> mockCodeGenerator;
        private Mock<IGameLogic> mockGameLogic;
        private Mock<IInvitationSendHelper> mockInvitationHelper;
        private LobbyCoreContext coreContext;
        private LobbyLogic lobbyLogic;

        [TestInitialize]
        public void Setup()
        {
            BaseSetup();

            mockSession = new Mock<LobbySession>(mockLoggerHelper.Object);
            mockValidation = new Mock<LobbyValidationHelper>(mockLoggerHelper.Object);
            mockCodeGenerator = new Mock<LobbyCodeGeneratorHelper>();
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

        [TestMethod]
        public async Task TestLobbyCreateSuccess()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "testuser",
                HostNickname = "TestPlayer",
                MaxPlayers = 4
            };

            mockCodeGenerator
                .Setup(x => x.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Returns("ABC12");

            mockSession
                .Setup(x => x.LobbyExists(It.IsAny<string>()))
                .Returns(false);

            var result = await lobbyLogic.CreateLobby(settings);

            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task TestLobbyCreateReturnsLobbyCode()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "testuser",
                HostNickname = "TestPlayer",
                MaxPlayers = 4
            };

            mockCodeGenerator
                .Setup(x => x.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Returns("XYZ99");

            var result = await lobbyLogic.CreateLobby(settings);

            Assert.AreEqual("XYZ99", result.LobbyCode);
        }

        [TestMethod]
        public async Task TestLobbyCreateReturnsSuccessCode()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "testuser",
                HostNickname = "TestPlayer",
                MaxPlayers = 4
            };

            mockCodeGenerator
                .Setup(x => x.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Returns("CODE1");

            var result = await lobbyLogic.CreateLobby(settings);

            Assert.AreEqual(MatchCreationResultCode.MatchCreation_Success, result.ResultCode);
        }

        [TestMethod]
        public async Task TestLobbyCreateWithNullSettings()
        {
            var result = await lobbyLogic.CreateLobby(null);

            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public async Task TestLobbyCreateWithNullSettingsReturnsInvalidParametersCode()
        {
            var result = await lobbyLogic.CreateLobby(null);

            Assert.AreEqual(MatchCreationResultCode.MatchCreation_InvalidParameters, result.ResultCode);
        }

        [TestMethod]
        public async Task TestLobbyCreateCallsValidation()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "testuser",
                HostNickname = "TestPlayer",
                MaxPlayers = 4
            };

            mockCodeGenerator
                .Setup(x => x.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Returns("TEST1");

            await lobbyLogic.CreateLobby(settings);

            mockValidation.Verify(x => x.ValidateCreateLobby(settings), Times.Once);
        }

        [TestMethod]
        public async Task TestLobbyCreateCallsSessionCreate()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "testuser",
                HostNickname = "TestPlayer",
                MaxPlayers = 4
            };

            mockCodeGenerator
                .Setup(x => x.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Returns("TEST2");

            await lobbyLogic.CreateLobby(settings);

            mockSession.Verify(x => x.CreateLobby(
                "TEST2",
                It.IsAny<ActiveLobbyData>()),
                Times.Once);
        }

        [TestMethod]
        public async Task TestLobbyCreateHandlesInvalidOperationException()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "testuser",
                HostNickname = "TestPlayer",
                MaxPlayers = 4
            };

            mockCodeGenerator
                .Setup(x => x.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Throws<InvalidOperationException>();

            var result = await lobbyLogic.CreateLobby(settings);

            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public async Task TestLobbyCreateHandlesInvalidOperationExceptionReturnsServerBusy()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "testuser",
                HostNickname = "TestPlayer",
                MaxPlayers = 4
            };

            mockCodeGenerator
                .Setup(x => x.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Throws<InvalidOperationException>();

            var result = await lobbyLogic.CreateLobby(settings);

            Assert.AreEqual(MatchCreationResultCode.MatchCreation_ServerBusy, result.ResultCode);
        }

        [TestMethod]
        public async Task TestLobbyCreateHandlesArgumentException()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "testuser",
                HostNickname = "TestPlayer",
                MaxPlayers = 4
            };

            mockValidation
                .Setup(x => x.ValidateCreateLobby(It.IsAny<MatchSettings>()))
                .Throws<ArgumentException>();

            var result = await lobbyLogic.CreateLobby(settings);

            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public async Task TestLobbyCreateHandlesArgumentExceptionReturnsInvalidSettings()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "testuser",
                HostNickname = "TestPlayer",
                MaxPlayers = 4
            };

            mockValidation
                .Setup(x => x.ValidateCreateLobby(It.IsAny<MatchSettings>()))
                .Throws<ArgumentException>();

            var result = await lobbyLogic.CreateLobby(settings);

            Assert.AreEqual(MatchCreationResultCode.MatchCreation_InvalidSettings, result.ResultCode);
        }

        [TestMethod]
        public async Task TestLobbyCreateHandlesTimeoutException()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "testuser",
                HostNickname = "TestPlayer",
                MaxPlayers = 4
            };

            mockCodeGenerator
                .Setup(x => x.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Throws<TimeoutException>();

            var result = await lobbyLogic.CreateLobby(settings);

            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public async Task TestLobbyCreateHandlesTimeoutExceptionReturnsTimeout()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "testuser",
                HostNickname = "TestPlayer",
                MaxPlayers = 4
            };

            mockCodeGenerator
                .Setup(x => x.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Throws<TimeoutException>();

            var result = await lobbyLogic.CreateLobby(settings);

            Assert.AreEqual(MatchCreationResultCode.MatchCreation_Timeout, result.ResultCode);
        }
    }
}
