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
    public class LobbySendInvitationsTest : BaseTestClass
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
        public async Task TestSendInvitationsEmptyLobbyCodeReturnsFalse()
        {
            var guests = new List<string> { "friend1@email.com" };

            var result = await lobbyLogic.SendInvitations("", "Host", guests);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendInvitationsNullLobbyCodeReturnsFalse()
        {
            var guests = new List<string> { "friend1@email.com" };

            var result = await lobbyLogic.SendInvitations(null, "Host", guests);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendInvitationsWhitespaceLobbyCodeReturnsFalse()
        {
            var guests = new List<string> { "friend1@email.com" };

            var result = await lobbyLogic.SendInvitations("   ", "Host", guests);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendInvitationsEmptySenderReturnsFalse()
        {
            var guests = new List<string> { "friend1@email.com" };

            var result = await lobbyLogic.SendInvitations("ABC12", "", guests);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendInvitationsNullSenderReturnsFalse()
        {
            var guests = new List<string> { "friend1@email.com" };

            var result = await lobbyLogic.SendInvitations("ABC12", null, guests);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendInvitationsWhitespaceSenderReturnsFalse()
        {
            var guests = new List<string> { "friend1@email.com" };

            var result = await lobbyLogic.SendInvitations("ABC12", "   ", guests);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendInvitationsNullGuestsReturnsFalse()
        {
            var result = await lobbyLogic.SendInvitations("ABC12", "Host", null);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendInvitationsEmptyGuestsListReturnsFalse()
        {
            var guests = new List<string>();

            var result = await lobbyLogic.SendInvitations("ABC12", "Host", guests);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendInvitationsLobbyNotFoundReturnsFalse()
        {
            var guests = new List<string> { "friend1@email.com" };

            mockSession.Setup(s => s.GetLobby("WRONG"))
                       .Returns((ActiveLobbyData)null);

            var result = await lobbyLogic.SendInvitations("WRONG", "Host", guests);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendInvitationsValidationFailsReturnsFalse()
        {
            var guests = new List<string> { "invalid email" };
            var lobby = CreateLobby();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockValidation.Setup(v => v.ValidateInviteGuests(guests))
                         .Throws(new ArgumentException("Invalid email format"));

            var result = await lobbyLogic.SendInvitations("ABC12", "Host", guests);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendInvitationsCallsValidation()
        {
            var guests = new List<string> { "friend1@email.com" };
            var lobby = CreateLobby();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockInvitationHelper.Setup(h => h.SendInvitation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                               .ReturnsAsync(true);

            await lobbyLogic.SendInvitations("ABC12", "Host", guests);

            mockValidation.Verify(
                v => v.ValidateInviteGuests(guests),
                Times.Once);
        }

        [TestMethod]
        public async Task TestSendInvitationsCallsInvitationHelper()
        {
            var guests = new List<string> { "friend1@email.com", "friend2@email.com" };
            var lobby = CreateLobby();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockInvitationHelper.Setup(h => h.SendInvitation("ABC12", "Host", guests))
                               .ReturnsAsync(true);

            await lobbyLogic.SendInvitations("ABC12", "Host", guests);

            mockInvitationHelper.Verify(
                h => h.SendInvitation("ABC12", "Host", guests),
                Times.Once);
        }

        [TestMethod]
        public async Task TestSendInvitationsReturnsHelperResult()
        {
            var guests = new List<string> { "friend1@email.com" };
            var lobby = CreateLobby();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockInvitationHelper.Setup(h => h.SendInvitation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                               .ReturnsAsync(true);

            var result = await lobbyLogic.SendInvitations("ABC12", "Host", guests);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestSendInvitationsReturnsFalseWhenHelperFails()
        {
            var guests = new List<string> { "friend1@email.com" };
            var lobby = CreateLobby();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockInvitationHelper.Setup(h => h.SendInvitation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                               .ReturnsAsync(false);

            var result = await lobbyLogic.SendInvitations("ABC12", "Host", guests);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendInvitationsHandlesTimeoutException()
        {
            var guests = new List<string> { "friend1@email.com" };
            var lobby = CreateLobby();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockValidation.Setup(v => v.ValidateInviteGuests(It.IsAny<List<string>>()))
                         .Throws<TimeoutException>();

            var result = await lobbyLogic.SendInvitations("ABC12", "Host", guests);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendInvitationsLogsTimeoutWarning()
        {
            var guests = new List<string> { "friend1@email.com" };
            var lobby = CreateLobby();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockValidation.Setup(v => v.ValidateInviteGuests(It.IsAny<List<string>>()))
                         .Throws(new TimeoutException("Timeout occurred"));

            await lobbyLogic.SendInvitations("ABC12", "Host", guests);

            mockLoggerHelper.Verify(
                l => l.LogWarning(It.Is<string>(s => s.Contains("SendInvitations timeout"))),
                Times.Once);
        }

        [TestMethod]
        public async Task TestSendInvitationsHandlesUnexpectedException()
        {
            var guests = new List<string> { "friend1@email.com" };
            var lobby = CreateLobby();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockInvitationHelper.Setup(h => h.SendInvitation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                               .Throws(new Exception("Unexpected error"));

            var result = await lobbyLogic.SendInvitations("ABC12", "Host", guests);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestSendInvitationsLogsUnexpectedError()
        {
            var guests = new List<string> { "friend1@email.com" };
            var lobby = CreateLobby();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockInvitationHelper.Setup(h => h.SendInvitation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                               .Throws(new Exception("Unexpected error"));

            await lobbyLogic.SendInvitations("ABC12", "Host", guests);

            mockLoggerHelper.Verify(
                l => l.LogError(It.Is<string>(s => s.Contains("Unexpected error sending invitations")), It.IsAny<Exception>()),
                Times.Once);
        }

        [TestMethod]
        public async Task TestSendInvitationsWithMultipleGuests()
        {
            var guests = new List<string> { "friend1@email.com", "friend2@email.com", "friend3@email.com" };
            var lobby = CreateLobby();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockInvitationHelper.Setup(h => h.SendInvitation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                               .ReturnsAsync(true);

            var result = await lobbyLogic.SendInvitations("ABC12", "Host", guests);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestSendInvitationsDoesNotCallHelperWhenValidationFails()
        {
            var guests = new List<string> { "invalid" };
            var lobby = CreateLobby();

            mockSession.Setup(s => s.GetLobby("ABC12")).Returns(lobby);
            mockValidation.Setup(v => v.ValidateInviteGuests(guests))
                         .Throws(new ArgumentException("Invalid format"));

            await lobbyLogic.SendInvitations("ABC12", "Host", guests);

            mockInvitationHelper.Verify(
                h => h.SendInvitation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()),
                Times.Never);
        }

        [TestMethod]
        public async Task TestSendInvitationsDoesNotCallHelperWhenLobbyNotFound()
        {
            var guests = new List<string> { "friend1@email.com" };

            mockSession.Setup(s => s.GetLobby("ABC12"))
                       .Returns((ActiveLobbyData)null);

            await lobbyLogic.SendInvitations("ABC12", "Host", guests);

            mockInvitationHelper.Verify(
                h => h.SendInvitation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()),
                Times.Never);
        }
    }
}
