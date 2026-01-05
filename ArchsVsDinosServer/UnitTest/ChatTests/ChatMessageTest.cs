using ArchsVsDinosServer;
using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Services.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts;
using Contracts.DTO;
using Contracts.DTO.Result_Codes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.ChatTests
{
    [TestClass]
    public class ChatMessageTest : BaseTestClass
    {
        private Mock<ILobbyServiceNotifier> mockLobbyNotifier;
        private Mock<IGameServiceNotifier> mockGameNotifier;
        private Mock<ICallbackProvider> mockCallbackProvider;
        private Mock<IChatManagerCallback> mockCallback;
        private Mock<IModerationManager> mockModerationManager;
        private Chat chat;
        private FieldInfo connectedUsersField;

        [TestInitialize]
        public void Setup()
        {
            base.BaseSetup();

            mockLobbyNotifier = new Mock<ILobbyServiceNotifier>();
            mockGameNotifier = new Mock<IGameServiceNotifier>();
            mockCallbackProvider = new Mock<ICallbackProvider>();
            mockCallback = new Mock<IChatManagerCallback>();
            mockModerationManager = new Mock<IModerationManager>();

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback.Object);

            ChatServiceDependencies dependencies = new ChatServiceDependencies
            {
                LoggerHelper = mockLoggerHelper.Object,
                ContextFactory = () => mockDbContext.Object,
                CallbackProvider = mockCallbackProvider.Object,
                ModerationManager = mockModerationManager.Object
            };

            chat = new Chat(
                dependencies,
                mockLobbyNotifier.Object,
                mockGameNotifier.Object
            );

            connectedUsersField = typeof(Chat).GetField("ConnectedUsers",
                BindingFlags.NonPublic | BindingFlags.Static);
            ClearConnectedUsers();
        }

        [TestCleanup]
        public void Cleanup()
        {
            ClearConnectedUsers();
        }

        [TestMethod]
        public void TestSendMessageWithEmptyUsernameDoesNotSend()
        {
            string username = "";
            string message = "Hello";

            chat.SendMessageToRoom(message, username);

            mockCallback.Verify(c => c.ReceiveMessage(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void TestSendMessageWithEmptyMessageDoesNotSend()
        {
            string username = "user1";
            string message = "";

            chat.SendMessageToRoom(message, username);

            mockCallback.Verify(c => c.ReceiveMessage(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void TestSendMessageWithUserNotConnectedDoesNotSend()
        {
            string username = "user1";
            string message = "Hello";

            chat.SendMessageToRoom(message, username);

            mockCallback.Verify(c => c.ReceiveMessage(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void TestSendMessageSuccessCallsReceiveMessage()
        {
            string username = "user1";
            string message = "Hello everyone";
            string lobbyCode = "LOBBY123";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            SetupMockUserSet(new List<UserAccount> { user });

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = true,
                    ShouldBan = false,
                    CurrentStrikes = 0,
                    Reason = ""
                });

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = lobbyCode
            };
            chat.Connect(request);

            chat.SendMessageToRoom(message, username);

            mockCallback.Verify(c => c.ReceiveMessage(
                "Lobby",
                username,
                message), Times.Once);
        }

        [TestMethod]
        public void TestSendMessageBroadcastsToFirstUser()
        {
            var mockCallback1 = new Mock<IChatManagerCallback>();
            var mockCallback2 = new Mock<IChatManagerCallback>();

            string username1 = "user1";
            string username2 = "user2";
            string message = "Hello all";
            string lobbyCode = "LOBBY123";

            UserAccount user1 = new UserAccount { idUser = 1, username = username1 };
            UserAccount user2 = new UserAccount { idUser = 2, username = username2 };

            SetupMockUserSet(new List<UserAccount> { user1, user2 });

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = true,
                    ShouldBan = false,
                    CurrentStrikes = 0,
                    Reason = ""
                });

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback1.Object);
            ChatConnectionRequest request1 = new ChatConnectionRequest { Username = username1, Context = 0, MatchCode = lobbyCode };
            chat.Connect(request1);

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback2.Object);
            ChatConnectionRequest request2 = new ChatConnectionRequest { Username = username2, Context = 0, MatchCode = lobbyCode };
            chat.Connect(request2);

            chat.SendMessageToRoom(message, username1);

            mockCallback1.Verify(c => c.ReceiveMessage("Lobby", username1, message), Times.Once);
        }

        [TestMethod]
        public void TestSendMessageBroadcastsToSecondUser()
        {
            var mockCallback1 = new Mock<IChatManagerCallback>();
            var mockCallback2 = new Mock<IChatManagerCallback>();

            string username1 = "user1";
            string username2 = "user2";
            string message = "Hello all";
            string lobbyCode = "LOBBY123";

            UserAccount user1 = new UserAccount { idUser = 1, username = username1 };
            UserAccount user2 = new UserAccount { idUser = 2, username = username2 };

            SetupMockUserSet(new List<UserAccount> { user1, user2 });

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = true,
                    ShouldBan = false,
                    CurrentStrikes = 0,
                    Reason = ""
                });

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback1.Object);
            ChatConnectionRequest request1 = new ChatConnectionRequest { Username = username1, Context = 0, MatchCode = lobbyCode };
            chat.Connect(request1);

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback2.Object);
            ChatConnectionRequest request2 = new ChatConnectionRequest { Username = username2, Context = 0, MatchCode = lobbyCode };
            chat.Connect(request2);

            chat.SendMessageToRoom(message, username1);

            mockCallback2.Verify(c => c.ReceiveMessage("Lobby", username1, message), Times.Once);
        }

        private void ClearConnectedUsers()
        {
            if (connectedUsersField != null)
            {
                var connectedUsers = connectedUsersField.GetValue(null);
                if (connectedUsers != null)
                {
                    var clearMethod = connectedUsers.GetType().GetMethod("Clear");
                    clearMethod?.Invoke(connectedUsers, null);
                }
            }
        }

        private int GetConnectedUsersCount()
        {
            if (connectedUsersField != null)
            {
                var connectedUsers = connectedUsersField.GetValue(null);
                if (connectedUsers != null)
                {
                    var countProperty = connectedUsers.GetType().GetProperty("Count");
                    return (int)(countProperty?.GetValue(connectedUsers) ?? 0);
                }
            }
            return 0;
        }
    }
}
