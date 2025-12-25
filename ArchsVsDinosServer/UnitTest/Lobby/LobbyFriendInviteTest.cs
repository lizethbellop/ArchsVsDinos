using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Game;
using ArchsVsDinosServer.Model;
using Contracts;
using Contracts.DTO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.ServiceModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.Lobby
{
    [TestClass]
    public class LobbyFriendInviteTest : BaseTestClass
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
        public async Task TestSendLobbyInviteToFriendSuccess()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host",
                MaxPlayers = 4
            };
            var lobby = new ActiveLobbyData("ABC12", settings);

            var mockCallback = new Mock<ILobbyManagerCallback>();

            mockSession
                .Setup(x => x.GetLobby("ABC12"))
                .Returns(lobby);

            mockSession
                .Setup(x => x.FindUserCallbackInAnyLobby("friend1"))
                .Returns(mockCallback.Object);

            var result = await lobbyLogic.SendLobbyInviteToFriend("ABC12", "Host", "friend1");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteToFriendWithEmptyLobbyCode()
        {
            var result = await lobbyLogic.SendLobbyInviteToFriend("", "Host", "friend1");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteToFriendWithEmptySender()
        {
            var result = await lobbyLogic.SendLobbyInviteToFriend("ABC12", "", "friend1");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteToFriendWithEmptyTarget()
        {
            var result = await lobbyLogic.SendLobbyInviteToFriend("ABC12", "Host", "");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteToFriendWhenLobbyNotFound()
        {
            mockSession
                .Setup(x => x.GetLobby("WRONG"))
                .Returns((ActiveLobbyData)null);

            var result = await lobbyLogic.SendLobbyInviteToFriend("WRONG", "Host", "friend1");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteToFriendWhenUserNotConnected()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host",
                MaxPlayers = 4
            };
            var lobby = new ActiveLobbyData("ABC12", settings);

            mockSession
                .Setup(x => x.GetLobby("ABC12"))
                .Returns(lobby);

            mockSession
                .Setup(x => x.FindUserCallbackInAnyLobby("friend1"))
                .Returns((ILobbyManagerCallback)null);

            var result = await lobbyLogic.SendLobbyInviteToFriend("ABC12", "Host", "friend1");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteToFriendCallsCallback()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host",
                MaxPlayers = 4
            };
            var lobby = new ActiveLobbyData("ABC12", settings);

            var mockCallback = new Mock<ILobbyManagerCallback>();

            mockSession
                .Setup(x => x.GetLobby("ABC12"))
                .Returns(lobby);

            mockSession
                .Setup(x => x.FindUserCallbackInAnyLobby("friend1"))
                .Returns(mockCallback.Object);

            await lobbyLogic.SendLobbyInviteToFriend("ABC12", "Host", "friend1");

            mockCallback.Verify(x => x.LobbyInvitationReceived(It.IsAny<LobbyInvitationDTO>()), Times.Once);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteToFriendCreatesCorrectInvitationDTO()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host",
                MaxPlayers = 4
            };
            var lobby = new ActiveLobbyData("ABC12", settings);

            var mockCallback = new Mock<ILobbyManagerCallback>();
            LobbyInvitationDTO capturedInvitation = null;

            mockCallback
                .Setup(x => x.LobbyInvitationReceived(It.IsAny<LobbyInvitationDTO>()))
                .Callback<LobbyInvitationDTO>(inv => capturedInvitation = inv);

            mockSession
                .Setup(x => x.GetLobby("ABC12"))
                .Returns(lobby);

            mockSession
                .Setup(x => x.FindUserCallbackInAnyLobby("friend1"))
                .Returns(mockCallback.Object);

            await lobbyLogic.SendLobbyInviteToFriend("ABC12", "Host", "friend1");

            Assert.IsNotNull(capturedInvitation);
            Assert.AreEqual("ABC12", capturedInvitation.LobbyCode);
            Assert.AreEqual("Host", capturedInvitation.SenderNickname);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteToFriendHandlesCommunicationException()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host",
                MaxPlayers = 4
            };
            var lobby = new ActiveLobbyData("ABC12", settings);

            var mockCallback = new Mock<ILobbyManagerCallback>();
            mockCallback
                .Setup(x => x.LobbyInvitationReceived(It.IsAny<LobbyInvitationDTO>()))
                .Throws<CommunicationException>();

            mockSession
                .Setup(x => x.GetLobby("ABC12"))
                .Returns(lobby);

            mockSession
                .Setup(x => x.FindUserCallbackInAnyLobby("friend1"))
                .Returns(mockCallback.Object);

            var result = await lobbyLogic.SendLobbyInviteToFriend("ABC12", "Host", "friend1");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteToFriendHandlesTimeoutException()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host",
                MaxPlayers = 4
            };
            var lobby = new ActiveLobbyData("ABC12", settings);

            var mockCallback = new Mock<ILobbyManagerCallback>();
            mockCallback
                .Setup(x => x.LobbyInvitationReceived(It.IsAny<LobbyInvitationDTO>()))
                .Throws<TimeoutException>();

            mockSession
                .Setup(x => x.GetLobby("ABC12"))
                .Returns(lobby);

            mockSession
                .Setup(x => x.FindUserCallbackInAnyLobby("friend1"))
                .Returns(mockCallback.Object);

            var result = await lobbyLogic.SendLobbyInviteToFriend("ABC12", "Host", "friend1");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteToFriendHandlesObjectDisposedException()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host",
                MaxPlayers = 4
            };
            var lobby = new ActiveLobbyData("ABC12", settings);

            var mockCallback = new Mock<ILobbyManagerCallback>();
            mockCallback
                .Setup(x => x.LobbyInvitationReceived(It.IsAny<LobbyInvitationDTO>()))
                .Throws(new ObjectDisposedException("ILobbyManagerCallback"));

            mockSession
                .Setup(x => x.GetLobby("ABC12"))
                .Returns(lobby);

            mockSession
                .Setup(x => x.FindUserCallbackInAnyLobby("friend1"))
                .Returns(mockCallback.Object);

            var result = await lobbyLogic.SendLobbyInviteToFriend("ABC12", "Host", "friend1");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteToFriendHandlesArgumentNullException()
        {
            mockSession
                .Setup(x => x.GetLobby(It.IsAny<string>()))
                .Throws<ArgumentNullException>();

            var result = await lobbyLogic.SendLobbyInviteToFriend("ABC12", "Host", "friend1");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteToFriendHandlesInvalidOperationException()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host",
                MaxPlayers = 4
            };
            var lobby = new ActiveLobbyData("ABC12", settings);

            mockSession
                .Setup(x => x.GetLobby("ABC12"))
                .Returns(lobby);

            mockSession
                .Setup(x => x.FindUserCallbackInAnyLobby(It.IsAny<string>()))
                .Throws<InvalidOperationException>();

            var result = await lobbyLogic.SendLobbyInviteToFriend("ABC12", "Host", "friend1");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendLobbyInviteToFriendHandlesGeneralException()
        {
            var settings = new MatchSettings
            {
                HostUserId = 1,
                HostUsername = "host",
                HostNickname = "Host",
                MaxPlayers = 4
            };
            var lobby = new ActiveLobbyData("ABC12", settings);

            mockSession
                .Setup(x => x.GetLobby("ABC12"))
                .Returns(lobby);

            mockSession
                .Setup(x => x.FindUserCallbackInAnyLobby(It.IsAny<string>()))
                .Throws<Exception>();

            var result = await lobbyLogic.SendLobbyInviteToFriend("ABC12", "Host", "friend1");

            Assert.IsFalse(result);
        }
    }
}
