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
    public class LobbyGameStartTest : BaseTestClass
    {
        private Mock<ILobbyValidationHelper> mockValidation;
        private Mock<ILobbyCodeGeneratorHelper> mockCodeGenerator;
        private Mock<ILobbySession> mockSession;
        private Mock<IGameLogic> mockGameLogic;
        private Mock<IInvitationSendHelper> mockInvitationHelper;

        private LobbyLogic lobbyLogic;

        [TestInitialize]
        public void Setup()
        {
            BaseSetup();

            mockValidation = new Mock<ILobbyValidationHelper>();
            mockCodeGenerator = new Mock<ILobbyCodeGeneratorHelper>();
            mockSession = new Mock<ILobbySession>();
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

        private ActiveLobbyData CreateLobby(int players)
        {
            var lobby = new ActiveLobbyData("ABC12", new MatchSettings
            {
                HostUserId = 100,
                HostUsername = "host",
                HostNickname = "Host",
                MaxPlayers = 4
            });

            lobby.AddPlayer(100, "host", "Host");

            for (int i = 1; i < players; i++)
            {
                lobby.AddPlayer(100 + i, $"user{i}", $"Player{i}");
            }

            return lobby;
        }

        [TestMethod]
        public async Task TestEvaluateGameStartLobbyNotFoundDoesNotInitializeMatch()
        {
            mockSession.Setup(s => s.GetLobby("ABC12"))
                       .Returns((ActiveLobbyData)null);

            await lobbyLogic.EvaluateGameStart("ABC12", 100);

            mockGameLogic.Verify(
                g => g.InitializeMatch(It.IsAny<string>(), It.IsAny<List<GamePlayerInitDTO>>()),
                Times.Never);
        }

        [TestMethod]
        public async Task TestEvaluateGameStartUserNotHostDoesNotInitializeMatch()
        {
            var lobby = CreateLobby(2);
            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            await lobbyLogic.EvaluateGameStart("ABC12", 999);

            mockGameLogic.Verify(
                g => g.InitializeMatch(It.IsAny<string>(), It.IsAny<List<GamePlayerInitDTO>>()),
                Times.Never);
        }

        [TestMethod]
        public async Task TestEvaluateGameStartNotEnoughPlayersDoesNotInitializeMatch()
        {
            var lobby = CreateLobby(1);
            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            await lobbyLogic.EvaluateGameStart("ABC12", 100);

            mockGameLogic.Verify(
                g => g.InitializeMatch(It.IsAny<string>(), It.IsAny<List<GamePlayerInitDTO>>()),
                Times.Never);
        }

        [TestMethod]
        public async Task TestEvaluateGameStartValidConditionsCallsInitializeMatch()
        {
            var lobby = CreateLobby(2);
            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            mockGameLogic
                .Setup(g => g.InitializeMatch("ABC12", It.IsAny<List<GamePlayerInitDTO>>()))
                .ReturnsAsync(true);

            await lobbyLogic.EvaluateGameStart("ABC12", 100);

            mockGameLogic.Verify(
                g => g.InitializeMatch("ABC12", It.IsAny<List<GamePlayerInitDTO>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task TestEvaluateGameStartGameCreationFailsBroadcastsOnce()
        {
            var lobby = CreateLobby(2);
            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            mockGameLogic
                .Setup(g => g.InitializeMatch("ABC12", It.IsAny<List<GamePlayerInitDTO>>()))
                .ReturnsAsync(false);

            await lobbyLogic.EvaluateGameStart("ABC12", 100);

            mockSession.Verify(
                s => s.Broadcast("ABC12", It.IsAny<Action<ILobbyManagerCallback>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task TestEvaluateGameStartPassesCorrectPlayerCount()
        {
            var lobby = CreateLobby(3);
            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            mockGameLogic
                .Setup(g => g.InitializeMatch(
                    "ABC12",
                    It.Is<List<GamePlayerInitDTO>>(p => p.Count == 3)))
                .ReturnsAsync(true);

            await lobbyLogic.EvaluateGameStart("ABC12", 100);

            mockGameLogic.Verify(
                g => g.InitializeMatch(
                    "ABC12",
                    It.Is<List<GamePlayerInitDTO>>(p => p.Count == 3)),
                Times.Once);
        }

        [TestMethod]
        public async Task TestEvaluateGameStartGameCreatedBroadcastsTwice()
        {
            var lobby = CreateLobby(2);
            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);

            mockGameLogic
                .Setup(g => g.InitializeMatch("ABC12", It.IsAny<List<GamePlayerInitDTO>>()))
                .ReturnsAsync(true);

            await lobbyLogic.EvaluateGameStart("ABC12", 100);

            mockSession.Verify(
                s => s.Broadcast("ABC12", It.IsAny<Action<ILobbyManagerCallback>>()),
                Times.Exactly(2));
        }
    }

}
