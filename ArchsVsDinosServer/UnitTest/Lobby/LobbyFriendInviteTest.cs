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
    public class LobbyFriendInviteTest : BaseTestClass
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

        private ActiveLobbyData CreateLobby()
        {
            return new ActiveLobbyData("ABC12", new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host",
                MaxPlayers = 4
            });
        }

        [TestMethod]
        public async Task TestSendLobbyInviteEmptyLobbyCodeReturnsFalse()
        {
            var result = await lobbyLogic.SendLobbyInviteToFriend("", "Host", "friend1");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteEmptySenderReturnsFalse()
        {
            var result = await lobbyLogic.SendLobbyInviteToFriend("ABC12", "", "friend1");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteEmptyTargetReturnsFalse()
        {
            var result = await lobbyLogic.SendLobbyInviteToFriend("ABC12", "Host", "");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteLobbyNotFoundReturnsFalse()
        {
            mockSession.Setup(s => s.GetLobby("WRONG"))
                       .Returns((ActiveLobbyData)null);

            var result = await lobbyLogic.SendLobbyInviteToFriend("WRONG", "Host", "friend1");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteTargetNotConnectedReturnsTrue()
        {
            var lobby = CreateLobby();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockSession.Setup(s => s.FindUserCallbackInAnyLobby("friend1"))
                       .Returns((ILobbyManagerCallback)null);

            var result = await lobbyLogic.SendLobbyInviteToFriend("ABC12", "Host", "friend1");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteCallsCallbackOnce()
        {
            var lobby = CreateLobby();
            var mockCallback = new Mock<ILobbyManagerCallback>();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockSession.Setup(s => s.FindUserCallbackInAnyLobby("friend1"))
                       .Returns(mockCallback.Object);

            await lobbyLogic.SendLobbyInviteToFriend("ABC12", "Host", "friend1");

            mockCallback.Verify(
                c => c.LobbyInvitationReceived(It.IsAny<LobbyInvitationDTO>()),
                Times.Once);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteCallbackReceivesCorrectInvitation()
        {
            var lobby = CreateLobby();
            var mockCallback = new Mock<ILobbyManagerCallback>();
            LobbyInvitationDTO capturedDto = null;

            mockCallback
                .Setup(c => c.LobbyInvitationReceived(It.IsAny<LobbyInvitationDTO>()))
                .Callback<LobbyInvitationDTO>(dto => capturedDto = dto);

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockSession.Setup(s => s.FindUserCallbackInAnyLobby("friend1"))
                       .Returns(mockCallback.Object);

            await lobbyLogic.SendLobbyInviteToFriend("ABC12", "Host", "friend1");

            Assert.AreEqual("ABC12", capturedDto.LobbyCode);
            Assert.AreEqual("Host", capturedDto.SenderNickname);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteCallbackThrowsCommunicationExceptionReturnsTrue()
        {
            var lobby = CreateLobby();
            var mockCallback = new Mock<ILobbyManagerCallback>();

            mockCallback
                .Setup(c => c.LobbyInvitationReceived(It.IsAny<LobbyInvitationDTO>()))
                .Throws<CommunicationException>();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockSession.Setup(s => s.FindUserCallbackInAnyLobby("friend1"))
                       .Returns(mockCallback.Object);

            var result = await lobbyLogic.SendLobbyInviteToFriend("ABC12", "Host", "friend1");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteCallbackThrowsTimeoutExceptionReturnsTrue()
        {
            var lobby = CreateLobby();
            var mockCallback = new Mock<ILobbyManagerCallback>();

            mockCallback
                .Setup(c => c.LobbyInvitationReceived(It.IsAny<LobbyInvitationDTO>()))
                .Throws<TimeoutException>();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockSession.Setup(s => s.FindUserCallbackInAnyLobby("friend1"))
                       .Returns(mockCallback.Object);

            var result = await lobbyLogic.SendLobbyInviteToFriend("ABC12", "Host", "friend1");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteArgumentNullExceptionReturnsFalse()
        {
            mockSession.Setup(s => s.GetLobby(It.IsAny<string>()))
                       .Throws<ArgumentNullException>();

            var result = await lobbyLogic.SendLobbyInviteToFriend("ABC12", "Host", "friend1");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteInvalidOperationExceptionReturnsFalse()
        {
            var lobby = CreateLobby();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockSession.Setup(s => s.FindUserCallbackInAnyLobby(It.IsAny<string>()))
                       .Throws<InvalidOperationException>();

            var result = await lobbyLogic.SendLobbyInviteToFriend("ABC12", "Host", "friend1");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteUnexpectedExceptionReturnsFalse()
        {
            var lobby = CreateLobby();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockSession.Setup(s => s.FindUserCallbackInAnyLobby(It.IsAny<string>()))
                       .Throws<Exception>();

            var result = await lobbyLogic.SendLobbyInviteToFriend("ABC12", "Host", "friend1");
            Assert.IsFalse(result);
        }
    }

}
