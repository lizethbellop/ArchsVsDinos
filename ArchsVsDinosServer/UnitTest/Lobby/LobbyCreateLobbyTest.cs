using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Game;
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
    public class LobbyCreateLobbyTest : BaseTestClass
    {
        /*private Mock<LobbyCoreContext> mockCore;
        private Mock<IGameLogic> mockGameLogic;
        private Mock<IInvitationSendHelper> mockInvitationHelper;
        private Mock<ILobbyValidation> mockValidation;
        private Mock<ICodeGenerator> mockCodeGenerator;
        private Mock<ILobbySession> mockSession;
        private LobbyLogic lobbyLogic;

        [TestInitialize]
        public void Setup()
        {
            BaseSetup();

            mockValidation = new Mock<ILobbyValidation>();
            mockCodeGenerator = new Mock<ICodeGenerator>();
            mockSession = new Mock<ISessionManager>();
            mockGameLogic = new Mock<IGameLogic>();
            mockInvitationHelper = new Mock<IInvitationSendHelper>();

            mockCore = new Mock<LobbyCoreContext>();
            mockCore.Setup(c => c.Validation).Returns(mockValidation.Object);
            mockCore.Setup(c => c.CodeGenerator).Returns(mockCodeGenerator.Object);
            mockCore.Setup(c => c.Session).Returns(mockSession.Object);

            lobbyLogic = new LobbyLogic(
                mockCore.Object,
                mockLoggerHelper.Object,
                mockGameLogic.Object,
                mockInvitationHelper.Object
            );
        }

        [TestMethod]
        public async Task TestLobbyCreacionExitosa()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "user1",
                HostNickname = "Player1"
            };

            mockCodeGenerator
                .Setup(c => c.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Returns("ABC123");

            var result = await lobbyLogic.CreateLobby(settings);

            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task TestLobbyCreacionRetornaCodigoCorrecto()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "user1",
                HostNickname = "Player1"
            };

            mockCodeGenerator
                .Setup(c => c.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Returns("XYZ999");

            var result = await lobbyLogic.CreateLobby(settings);

            Assert.AreEqual("XYZ999", result.LobbyCode);
        }

        [TestMethod]
        public async Task TestLobbyCreacionRetornaCodigoExito()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "user1",
                HostNickname = "Player1"
            };

            mockCodeGenerator
                .Setup(c => c.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Returns("ABC123");

            var result = await lobbyLogic.CreateLobby(settings);

            Assert.AreEqual(MatchCreationResultCode.MatchCreation_Success, result.ResultCode);
        }

        [TestMethod]
        public async Task TestLobbyCreacionConSettingsNulos()
        {
            var result = await lobbyLogic.CreateLobby(null);

            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public async Task TestLobbyCreacionConSettingsNulosRetornaCodigoParametrosInvalidos()
        {
            var result = await lobbyLogic.CreateLobby(null);

            Assert.AreEqual(MatchCreationResultCode.MatchCreation_InvalidParameters, result.ResultCode);
        }

        [TestMethod]
        public async Task TestLobbyCreacionLlamaValidacion()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "user1",
                HostNickname = "Player1"
            };

            mockCodeGenerator
                .Setup(c => c.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Returns("ABC123");

            await lobbyLogic.CreateLobby(settings);

            mockValidation.Verify(v => v.ValidateCreateLobby(settings), Times.Once);
        }

        [TestMethod]
        public async Task TestLobbyCreacionGuardaEnSesion()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "user1",
                HostNickname = "Player1"
            };

            mockCodeGenerator
                .Setup(c => c.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Returns("ABC123");

            await lobbyLogic.CreateLobby(settings);

            mockSession.Verify(s => s.CreateLobby("ABC123", It.IsAny<ActiveLobbyData>()), Times.Once);
        }

        [TestMethod]
        public async Task TestLobbyCreacionConErrorGeneracionCodigo()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "user1",
                HostNickname = "Player1"
            };

            mockCodeGenerator
                .Setup(c => c.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Throws<InvalidOperationException>();

            var result = await lobbyLogic.CreateLobby(settings);

            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public async Task TestLobbyCreacionConErrorGeneracionCodigoRetornaServidorOcupado()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "user1",
                HostNickname = "Player1"
            };

            mockCodeGenerator
                .Setup(c => c.GenerateLobbyCode(It.IsAny<Func<string, bool>>()))
                .Throws<InvalidOperationException>();

            var result = await lobbyLogic.CreateLobby(settings);

            Assert.AreEqual(MatchCreationResultCode.MatchCreation_ServerBusy, result.ResultCode);
        }

        [TestMethod]
        public async Task TestLobbyCreacionConValidacionInvalida()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "user1",
                HostNickname = "Player1"
            };

            mockValidation
                .Setup(v => v.ValidateCreateLobby(It.IsAny<MatchSettings>()))
                .Throws<ArgumentException>();

            var result = await lobbyLogic.CreateLobby(settings);

            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public async Task TestLobbyCreacionConValidacionInvalidaRetornaSettingsInvalidos()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "user1",
                HostNickname = "Player1"
            };

            mockValidation
                .Setup(v => v.ValidateCreateLobby(It.IsAny<MatchSettings>()))
                .Throws<ArgumentException>();

            var result = await lobbyLogic.CreateLobby(settings);

            Assert.AreEqual(MatchCreationResultCode.MatchCreation_InvalidSettings, result.ResultCode);
        }

        [TestMethod]
        public async Task TestLobbyCreacionConTimeout()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "user1",
                HostNickname = "Player1"
            };

            mockValidation
                .Setup(v => v.ValidateCreateLobby(It.IsAny<MatchSettings>()))
                .Throws<TimeoutException>();

            var result = await lobbyLogic.CreateLobby(settings);

            Assert.IsFalse(result.Success);
        }

        [TestMethod]
        public async Task TestLobbyCreacionConTimeoutRetornaCodigoTimeout()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "user1",
                HostNickname = "Player1"
            };

            mockValidation
                .Setup(v => v.ValidateCreateLobby(It.IsAny<MatchSettings>()))
                .Throws<TimeoutException>();

            var result = await lobbyLogic.CreateLobby(settings);

            Assert.AreEqual(MatchCreationResultCode.MatchCreation_Timeout, result.ResultCode);
        }*/
    }
}
