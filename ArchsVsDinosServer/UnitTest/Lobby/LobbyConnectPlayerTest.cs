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
    public class LobbyConnectPlayerTest : BaseTestClass
    {
        private Mock<ILobbySession> mockSession;
        private Mock<ILobbyValidationHelper> mockValidation;
        private Mock<ILobbyCodeGeneratorHelper> mockCodeGenerator;
        private Mock<IGameLogic> mockGameLogic;
        private Mock<IInvitationSendHelper> mockInvitationHelper;
        private Mock<ILobbyManagerCallback> mockCallback;

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
            mockCallback = new Mock<ILobbyManagerCallback>();

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

        private ActiveLobbyData CreateLobbyWithPlayer(string playerNickname)
        {
            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host",
                MaxPlayers = 4
            });
            lobby.AddPlayer(2, "player", playerNickname);
            return lobby;
        }

        [TestMethod]
        public void TestConnectPlayerEmptyLobbyCodeDoesNothing()
        {
            lobbyLogic.ConnectPlayer("", "Player1");

            mockSession.Verify(
                s => s.GetLobby(It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public void TestConnectPlayerEmptyNicknameDoesNothing()
        {
            lobbyLogic.ConnectPlayer("ABC12", "");

            mockSession.Verify(
                s => s.GetLobby(It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public void TestConnectPlayerNullLobbyCodeDoesNothing()
        {
            lobbyLogic.ConnectPlayer(null, "Player1");

            mockSession.Verify(
                s => s.GetLobby(It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public void TestConnectPlayerNullNicknameDoesNothing()
        {
            lobbyLogic.ConnectPlayer("ABC12", null);

            mockSession.Verify(
                s => s.GetLobby(It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public void TestConnectPlayerWhitespaceLobbyCodeDoesNothing()
        {
            lobbyLogic.ConnectPlayer("   ", "Player1");

            mockSession.Verify(
                s => s.GetLobby(It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public void TestConnectPlayerWhitespaceNicknameDoesNothing()
        {
            lobbyLogic.ConnectPlayer("ABC12", "   ");

            mockSession.Verify(
                s => s.GetLobby(It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public void TestConnectPlayerLobbyNotFoundDoesNotConnect()
        {
            mockSession.Setup(s => s.GetLobby("WRONG"))
                       .Returns((ActiveLobbyData)null);

            lobbyLogic.ConnectPlayer("WRONG", "Player1");

            mockSession.Verify(
                s => s.ConnectPlayerCallback(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ILobbyManagerCallback>()),
                Times.Never);
        }

        [TestMethod]
        public void TestConnectPlayerNotInLobbyPlayersListDoesNotConnect()
        {
            var lobby = CreateLobbyWithPlayer("OtherPlayer");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            lobbyLogic.ConnectPlayer("ABC12", "Player1");

            mockSession.Verify(
                s => s.ConnectPlayerCallback(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ILobbyManagerCallback>()),
                Times.Never);
        }

        [TestMethod]
        public void TestConnectPlayerDoesNotDisconnectIfSameLobby()
        {
            var lobby = CreateLobbyWithPlayer("Player1");

            mockSession.Setup(s => s.FindLobbyByPlayerNickname("Player1"))
                       .Returns(lobby);
            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            lobbyLogic.ConnectPlayer("ABC12", "Player1");

            mockSession.Verify(
                s => s.DisconnectPlayerCallback(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public void TestConnectPlayerExitsEarlyWithoutOperationContext()
        {
            var oldLobby = CreateLobbyWithPlayer("Player1");
            oldLobby.LobbyCode = "OLD99";
            var newLobby = CreateLobbyWithPlayer("Player1");
            newLobby.LobbyCode = "ABC12";

            mockSession.Setup(s => s.FindLobbyByPlayerNickname("Player1"))
                       .Returns(oldLobby);
            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(newLobby);

            lobbyLogic.ConnectPlayer("ABC12", "Player1");

            mockSession.Verify(
                s => s.DisconnectPlayerCallback(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public void TestConnectPlayerWithoutOperationContextLogsWarning()
        {
            var lobby = CreateLobbyWithPlayer("Player1");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            lobbyLogic.ConnectPlayer("ABC12", "Player1");

            mockLoggerHelper.Verify(
                l => l.LogWarning("RegisterConnection called without OperationContext."),
                Times.Once);
        }

        [TestMethod]
        public void TestConnectPlayerWithoutOperationContextDoesNotThrow()
        {
            var lobby = CreateLobbyWithPlayer("Player1");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            lobbyLogic.ConnectPlayer("ABC12", "Player1");

            mockLoggerHelper.Verify(
                l => l.LogWarning("RegisterConnection called without OperationContext."),
                Times.Once);
        }

        [TestMethod]
        public void TestConnectPlayerWithoutOperationContextExitsEarly()
        {
            var lobby = CreateLobbyWithPlayer("Player1");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            lobbyLogic.ConnectPlayer("ABC12", "Player1");

            mockLoggerHelper.Verify(
                l => l.LogWarning(It.Is<string>(s => s.Contains("Timeout"))),
                Times.Never);
        }

        [TestMethod]
        public void TestConnectPlayerLogsWarningWhenNoOperationContext()
        {
            var lobby = CreateLobbyWithPlayer("Player1");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            lobbyLogic.ConnectPlayer("ABC12", "Player1");

            mockLoggerHelper.Verify(
                l => l.LogWarning("RegisterConnection called without OperationContext."),
                Times.Once);
        }

        [TestMethod]
        public void TestConnectPlayerRequiresOperationContext()
        {
            var lobby = CreateLobbyWithPlayer("Player1");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockSession.Setup(s => s.FindLobbyByPlayerNickname("Player1"))
                       .Returns((ActiveLobbyData)null);

            lobbyLogic.ConnectPlayer("ABC12", "Player1");

            mockSession.Verify(
                s => s.ConnectPlayerCallback(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ILobbyManagerCallback>()),
                Times.Never);
        }
    }

}
