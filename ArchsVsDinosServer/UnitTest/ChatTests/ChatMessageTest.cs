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

        private Mock<ILobbyNotifier> mockLobbyNotifier;
        private Mock<IGameNotifier> mockGameNotifier;
        private Mock<ICallbackProvider> mockCallbackProvider;
        private Mock<IChatManagerCallback> mockCallback;
        private Chat chat;
        private FieldInfo connectedUsersField;

        [TestInitialize]
        public void Setup()
        {
            base.BaseSetup();

            mockLobbyNotifier = new Mock<ILobbyNotifier>();
            mockGameNotifier = new Mock<IGameNotifier>();
            mockCallbackProvider = new Mock<ICallbackProvider>();
            mockCallback = new Mock<IChatManagerCallback>();

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback.Object);

            BasicServiceDependencies dependencies = new BasicServiceDependencies
            {
                loggerHelper = mockLoggerHelper.Object,
                contextFactory = () => mockDbContext.Object,
                callbackProvider = mockCallbackProvider.Object
            };

            chat = new Chat(dependencies, mockLobbyNotifier.Object, mockGameNotifier.Object);

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
        public void TestSendMessageUsernameEmpty()
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
        public void TestSendMessageMessageEmpty()
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
        public void TestSendMessageUserNotConnected()
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
        public void TestSendMessageSuccess()
        {
            string username = "user1";
            string message = "Hello everyone";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            SetupMockUserSet(new List<UserAccount> { user });

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = null
            };
            chat.Connect(request);

            chat.SendMessageToRoom(message, username);

            mockCallback.Verify(c => c.ReceiveMessage(
                "Lobby",
                username,
                message), Times.Once);
        }

        [TestMethod]
        public void TestSendMessageBroadcastsToAllUsers()
        {
            var mockCallback1 = new Mock<IChatManagerCallback>();
            var mockCallback2 = new Mock<IChatManagerCallback>();

            string username1 = "user1";
            string username2 = "user2";
            string message = "Hello all";

            UserAccount user1 = new UserAccount { idUser = 1, username = username1 };
            UserAccount user2 = new UserAccount { idUser = 2, username = username2 };

            SetupMockUserSet(new List<UserAccount> { user1, user2 });

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback1.Object);
            ChatConnectionRequest request1 = new ChatConnectionRequest { Username = username1, Context = 0, MatchCode = null };
            chat.Connect(request1);

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback2.Object);
            ChatConnectionRequest request2 = new ChatConnectionRequest { Username = username2, Context = 0, MatchCode = null };
            chat.Connect(request2);

            chat.SendMessageToRoom(message, username1);

            mockCallback1.Verify(c => c.ReceiveMessage("Lobby", username1, message), Times.Once);
            mockCallback2.Verify(c => c.ReceiveMessage("Lobby", username1, message), Times.Once);
        }

        [TestMethod]
        public void TestSendMessageBlockedFirstStrike()
        {
            string username = "user1";
            string badMessage = "badword";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            SetupMockUserSet(new List<UserAccount> { user });

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = null
            };
            chat.Connect(request);

            chat.SendMessageToRoom(badMessage, username);

            mockCallback.Verify(c => c.ReceiveSystemNotification(
                ChatResultCode.Chat_MessageBlocked,
                It.Is<string>(s => s.Contains("Warning 1/3"))), Times.Once);

            mockCallback.Verify(c => c.ReceiveMessage(
                It.IsAny<string>(),
                It.IsAny<string>(),
                badMessage), Times.Never);
        }

        [TestMethod]
        public void TestSendMessageBlockedSecondStrike()
        {
            string username = "user1";
            string badMessage = "badword";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            SetupMockUserSet(new List<UserAccount> { user });

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = null
            };
            chat.Connect(request);

            chat.SendMessageToRoom(badMessage, username);
            chat.SendMessageToRoom(badMessage, username);

            mockCallback.Verify(c => c.ReceiveSystemNotification(
                ChatResultCode.Chat_MessageBlocked,
                It.Is<string>(s => s.Contains("Warning 2/3"))), Times.Once);
        }

        [TestMethod]
        public void TestSendMessageBannedThirdStrikeInLobby()
        {
            string username = "user1";
            string badMessage = "badword";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            SetupMockUserSet(new List<UserAccount> { user });

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = null
            };
            chat.Connect(request);

            chat.SendMessageToRoom(badMessage, username);
            chat.SendMessageToRoom(badMessage, username);
            chat.SendMessageToRoom(badMessage, username);

            mockCallback.Verify(c => c.UserBannedFromChat(username, 3), Times.Once);
            mockLobbyNotifier.Verify(n => n.NotifyPlayerExpelled(username, "Inappropriate language"), Times.Once);
            Assert.AreEqual(0, GetConnectedUsersCount());
        }

        [TestMethod]
        public void TestSendMessageBannedThirdStrikeInGame()
        {
            string username = "user1";
            string matchCode = "MATCH123";
            string badMessage = "badword";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            SetupMockUserSet(new List<UserAccount> { user });

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 1,
                MatchCode = matchCode
            };
            chat.Connect(request);

            chat.SendMessageToRoom(badMessage, username);
            chat.SendMessageToRoom(badMessage, username);
            chat.SendMessageToRoom(badMessage, username);

            mockCallback.Verify(c => c.UserBannedFromChat(username, 3), Times.Once);
            mockGameNotifier.Verify(n => n.NotifyPlayerExpelled(matchCode, username, "Inappropriate language"), Times.Once);
            Assert.AreEqual(0, GetConnectedUsersCount());
        }

        [TestMethod]
        public void TestSendMessageBanBroadcastsToAllUsers()
        {
            var mockCallback1 = new Mock<IChatManagerCallback>();
            var mockCallback2 = new Mock<IChatManagerCallback>();

            string username1 = "user1";
            string username2 = "user2";
            string badMessage = "badword";

            UserAccount user1 = new UserAccount { idUser = 1, username = username1 };
            UserAccount user2 = new UserAccount { idUser = 2, username = username2 };

            SetupMockUserSet(new List<UserAccount> { user1, user2 });

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback1.Object);
            ChatConnectionRequest request1 = new ChatConnectionRequest { Username = username1, Context = 0, MatchCode = null };
            chat.Connect(request1);

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback2.Object);
            ChatConnectionRequest request2 = new ChatConnectionRequest { Username = username2, Context = 0, MatchCode = null };
            chat.Connect(request2);

            chat.SendMessageToRoom(badMessage, username1);
            chat.SendMessageToRoom(badMessage, username1);
            chat.SendMessageToRoom(badMessage, username1);

            mockCallback2.Verify(c => c.ReceiveSystemNotification(
                ChatResultCode.Chat_UserBanned,
                It.Is<string>(s => s.Contains(username1) && s.Contains("expelled"))), Times.Once);
        }

        [TestMethod]
        public void TestSendMessageAfterBanUserRemoved()
        {
            string username = "user1";
            string badMessage = "badword";
            string normalMessage = "Hello";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            SetupMockUserSet(new List<UserAccount> { user });

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = null
            };
            chat.Connect(request);

            chat.SendMessageToRoom(badMessage, username);
            chat.SendMessageToRoom(badMessage, username);
            chat.SendMessageToRoom(badMessage, username);

            Assert.AreEqual(0, GetConnectedUsersCount());

            chat.SendMessageToRoom(normalMessage, username);

            mockCallback.Verify(c => c.ReceiveMessage(
                It.IsAny<string>(),
                It.IsAny<string>(),
                normalMessage), Times.Never);
        }

        [TestMethod]
        public void TestSendMessageBanTriggersMinimumPlayersCheckInLobby()
        {
            var mockCallback1 = new Mock<IChatManagerCallback>();
            var mockCallback2 = new Mock<IChatManagerCallback>();

            string username1 = "user1";
            string username2 = "user2";
            string badMessage = "badword";

            UserAccount user1 = new UserAccount { idUser = 1, username = username1 };
            UserAccount user2 = new UserAccount { idUser = 2, username = username2 };

            SetupMockUserSet(new List<UserAccount> { user1, user2 });

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback1.Object);
            ChatConnectionRequest request1 = new ChatConnectionRequest { Username = username1, Context = 0, MatchCode = null };
            chat.Connect(request1);

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback2.Object);
            ChatConnectionRequest request2 = new ChatConnectionRequest { Username = username2, Context = 0, MatchCode = null };
            chat.Connect(request2);

            chat.SendMessageToRoom(badMessage, username1);
            chat.SendMessageToRoom(badMessage, username1);
            chat.SendMessageToRoom(badMessage, username1);

            mockLobbyNotifier.Verify(n => n.NotifyLobbyClosure(It.IsAny<string>()), Times.Once);
        }

        private void ClearConnectedUsers()
        {
            if (connectedUsersField != null)
            {
                var connectedUsers = connectedUsersField.GetValue(null) as ConcurrentDictionary<string, object>;
                connectedUsers?.Clear();
            }
        }

        private int GetConnectedUsersCount()
        {
            if (connectedUsersField != null)
            {
                var connectedUsers = connectedUsersField.GetValue(null) as ConcurrentDictionary<string, object>;
                return connectedUsers?.Count ?? 0;
            }
            return 0;
        }
    }
}
