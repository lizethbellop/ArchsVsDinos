using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Game;
using ArchsVsDinosServer.Model;
using Contracts.DTO;
using Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ArchsVsDinosServer.Interfaces.Lobby;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.Lobby
{
    [TestClass]
    public class LobbyDisconnectPlayerTest : BaseTestClass
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
        public void TestDisconnectPlayerEmptyLobbyCodeDoesNothing()
        {
            lobbyLogic.DisconnectPlayer("", "Player1");

            mockSession.Verify(
                s => s.GetLobby(It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public void TestDisconnectPlayerEmptyNicknameDoesNothing()
        {
            lobbyLogic.DisconnectPlayer("ABC12", "");

            mockSession.Verify(
                s => s.GetLobby(It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public void TestDisconnectPlayerNullLobbyCodeDoesNothing()
        {
            lobbyLogic.DisconnectPlayer(null, "Player1");

            mockSession.Verify(
                s => s.GetLobby(It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public void TestDisconnectPlayerNullNicknameDoesNothing()
        {
            lobbyLogic.DisconnectPlayer("ABC12", null);

            mockSession.Verify(
                s => s.GetLobby(It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public void TestDisconnectPlayerWhitespaceLobbyCodeDoesNothing()
        {
            lobbyLogic.DisconnectPlayer("   ", "Player1");

            mockSession.Verify(
                s => s.GetLobby(It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public void TestDisconnectPlayerWhitespaceNicknameDoesNothing()
        {
            lobbyLogic.DisconnectPlayer("ABC12", "   ");

            mockSession.Verify(
                s => s.GetLobby(It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public void TestDisconnectPlayerLobbyNotFoundDoesNothing()
        {
            mockSession.Setup(s => s.GetLobby("WRONG"))
                       .Returns((ActiveLobbyData)null);

            lobbyLogic.DisconnectPlayer("WRONG", "Player1");

            mockSession.Verify(
                s => s.DisconnectPlayerCallback(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public void TestDisconnectPlayerNotInLobbyDoesNothing()
        {
            var lobby = CreateLobbyWithPlayers();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            lobbyLogic.DisconnectPlayer("ABC12", "NonExistentPlayer");

            mockSession.Verify(
                s => s.DisconnectPlayerCallback(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public void TestDisconnectPlayerCallsDisconnectPlayerCallback()
        {
            var lobby = CreateLobbyWithPlayers();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            lobbyLogic.DisconnectPlayer("ABC12", "Player2");

            mockSession.Verify(
                s => s.DisconnectPlayerCallback("ABC12", "Player2"),
                Times.Once);
        }

        [TestMethod]
        public void TestDisconnectPlayerRemovesPlayerFromLobby()
        {
            var lobby = CreateLobbyWithPlayers();
            var initialCount = lobby.Players.Count;

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            lobbyLogic.DisconnectPlayer("ABC12", "Player2");

            Assert.AreEqual(initialCount - 1, lobby.Players.Count);
        }

        [TestMethod]
        public void TestDisconnectPlayerRemovesLobbyWhenEmpty()
        {
            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host",
                MaxPlayers = 4
            });
            lobby.AddPlayer(1, "host", "Host");

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            lobbyLogic.DisconnectPlayer("ABC12", "Host");

            mockSession.Verify(
                s => s.RemoveLobby("ABC12"),
                Times.Once);
        }

        [TestMethod]
        public void TestDisconnectPlayerDoesNotRemoveLobbyWhenNotEmpty()
        {
            var lobby = CreateLobbyWithPlayers();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            lobbyLogic.DisconnectPlayer("ABC12", "Player2");

            mockSession.Verify(
                s => s.RemoveLobby(It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public void TestDisconnectHostTransfersToNextRegisteredPlayer()
        {
            var lobby = CreateLobbyWithPlayers();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            lobbyLogic.DisconnectPlayer("ABC12", "Host");

            Assert.AreEqual(2, lobby.HostUserId);
        }

        [TestMethod]
        public void TestDisconnectHostRemovesHostFromPlayersList()
        {
            var lobby = CreateLobbyWithPlayers();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            lobbyLogic.DisconnectPlayer("ABC12", "Host");

            // El host debe haberse removido
            Assert.IsFalse(lobby.Players.Any(p => p.Nickname == "Host"));
            Assert.AreEqual(2, lobby.Players.Count);
        }

        [TestMethod]
        public void TestDisconnectNonHostPlayerDoesNotTransferHost()
        {
            var lobby = CreateLobbyWithPlayers();
            var originalHostId = lobby.HostUserId;

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            lobbyLogic.DisconnectPlayer("ABC12", "Player2");

            Assert.AreEqual(originalHostId, lobby.HostUserId);
        }

        [TestMethod]
        public void TestDisconnectPlayerCaseInsensitiveNickname()
        {
            var lobby = CreateLobbyWithPlayers();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            lobbyLogic.DisconnectPlayer("ABC12", "pLaYeR2");

            mockSession.Verify(
                s => s.DisconnectPlayerCallback("ABC12", "pLaYeR2"),
                Times.Once);
        }

        [TestMethod]
        public void TestDisconnectPlayerLogsDbErrorWhenMappingFails()
        {
            var lobby = CreateLobbyWithPlayers();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            lobbyLogic.DisconnectPlayer("ABC12", "Player2");

            mockLoggerHelper.Verify(
                l => l.LogWarning(It.Is<string>(s =>
                    s.Contains("Error in Disconnect player") ||
                    s.Contains("[SERVER DB ERROR]"))),
                Times.AtLeastOnce);
        }
    }
}
