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
    public class ChatConnectionTest : BaseTestClass
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
        public void TestConnectUserNotFoundSendsErrorNotification()
        {
            string username = "nonexistent";
            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = "LOBBY123"
            };

            SetupMockUserSet(new List<UserAccount>());

            chat.Connect(request);

            mockCallback.Verify(c => c.ReceiveSystemNotification(
                ChatResultCode.Chat_Error,
                "User not found"), Times.Once);
        }

        [TestMethod]
        public void TestConnectUserNotFoundDoesNotAddUser()
        {
            string username = "nonexistent";
            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = "LOBBY123"
            };

            SetupMockUserSet(new List<UserAccount>());

            chat.Connect(request);

            Assert.AreEqual(0, GetConnectedUsersCount());
        }

        [TestMethod]
        public void TestConnectSuccessInLobbyAddsUser()
        {
            string username = "user1";
            string lobbyCode = "LOBBY123";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = lobbyCode
            };

            SetupMockUserSet(new List<UserAccount> { user });

            chat.Connect(request);

            Assert.AreEqual(1, GetConnectedUsersCount());
        }

        [TestMethod]
        public void TestConnectSuccessInLobbySendsNotification()
        {
            string username = "user1";
            string lobbyCode = "LOBBY123";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = lobbyCode
            };

            SetupMockUserSet(new List<UserAccount> { user });

            chat.Connect(request);

            mockCallback.Verify(c => c.ReceiveSystemNotification(
                ChatResultCode.Chat_UserConnected,
                It.Is<string>(s => s.Contains(username))), Times.Once);
        }

        [TestMethod]
        public void TestConnectSuccessInGameAddsUser()
        {
            string username = "user1";
            string matchCode = "MATCH123";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 1,
                MatchCode = matchCode
            };

            SetupMockUserSet(new List<UserAccount> { user });

            chat.Connect(request);

            Assert.AreEqual(1, GetConnectedUsersCount());
        }

        [TestMethod]
        public void TestConnectUserAlreadyConnectedSendsNotification()
        {
            string username = "user1";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = "LOBBY123"
            };

            SetupMockUserSet(new List<UserAccount> { user });

            chat.Connect(request);
            chat.Connect(request);

            mockCallback.Verify(c => c.ReceiveSystemNotification(
                ChatResultCode.Chat_UserAlreadyConnected,
                "User already connected"), Times.Once);
        }

        [TestMethod]
        public void TestConnectUserAlreadyConnectedDoesNotDuplicate()
        {
            string username = "user1";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = "LOBBY123"
            };

            SetupMockUserSet(new List<UserAccount> { user });

            chat.Connect(request);
            chat.Connect(request);

            Assert.AreEqual(1, GetConnectedUsersCount());
        }

        [TestMethod]
        public void TestConnectCallbackErrorDoesNotAddUser()
        {
            string username = "user1";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = "LOBBY123"
            };

            SetupMockUserSet(new List<UserAccount> { user });
            mockCallbackProvider.Setup(p => p.GetCallback())
                .Throws(new InvalidOperationException("Callback error"));

            chat.Connect(request);

            Assert.AreEqual(0, GetConnectedUsersCount());
        }

        [TestMethod]
        public void TestConnectUpdatesUserList()
        {
            string username = "user1";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = "LOBBY123"
            };

            SetupMockUserSet(new List<UserAccount> { user });

            chat.Connect(request);

            mockCallback.Verify(c => c.UpdateUserList(
                It.Is<List<string>>(list => list.Contains(username))), Times.AtLeastOnce);
        }

        [TestMethod]
        public void TestDisconnectRemovesUser()
        {
            var mockCallback1 = new Mock<IChatManagerCallback>();
            var mockCallback2 = new Mock<IChatManagerCallback>();

            string username1 = "user1";
            string username2 = "user2";
            string lobbyCode = "LOBBY123";

            UserAccount user1 = new UserAccount { idUser = 1, username = username1 };
            UserAccount user2 = new UserAccount { idUser = 2, username = username2 };

            SetupMockUserSet(new List<UserAccount> { user1, user2 });

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback1.Object);
            ChatConnectionRequest request1 = new ChatConnectionRequest { Username = username1, Context = 0, MatchCode = lobbyCode };
            chat.Connect(request1);

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback2.Object);
            ChatConnectionRequest request2 = new ChatConnectionRequest { Username = username2, Context = 0, MatchCode = lobbyCode };
            chat.Connect(request2);

            chat.Disconnect(username1);

            Assert.AreEqual(0, GetConnectedUsersCount());
        }

        [TestMethod]
        public void TestDisconnectNonExistentUserDoesNothing()
        {
            string username = "nonexistent";

            chat.Disconnect(username);

            Assert.AreEqual(0, GetConnectedUsersCount());
        }

        [TestMethod]
        public void TestDisconnectUpdatesUserList()
        {
            var mockCallback1 = new Mock<IChatManagerCallback>();
            var mockCallback2 = new Mock<IChatManagerCallback>();
            var mockCallback3 = new Mock<IChatManagerCallback>();

            string username1 = "user1";
            string username2 = "user2";
            string username3 = "user3";
            string lobbyCode = "LOBBY123";

            UserAccount user1 = new UserAccount { idUser = 1, username = username1 };
            UserAccount user2 = new UserAccount { idUser = 2, username = username2 };
            UserAccount user3 = new UserAccount { idUser = 3, username = username3 };

            SetupMockUserSet(new List<UserAccount> { user1, user2, user3 });

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback1.Object);
            ChatConnectionRequest request1 = new ChatConnectionRequest { Username = username1, Context = 0, MatchCode = lobbyCode };
            chat.Connect(request1);

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback2.Object);
            ChatConnectionRequest request2 = new ChatConnectionRequest { Username = username2, Context = 0, MatchCode = lobbyCode };
            chat.Connect(request2);

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback3.Object);
            ChatConnectionRequest request3 = new ChatConnectionRequest { Username = username3, Context = 0, MatchCode = lobbyCode };
            chat.Connect(request3);

            chat.Disconnect(username1);

            mockCallback2.Verify(c => c.UpdateUserList(
                It.Is<List<string>>(list => !list.Contains(username1))), Times.AtLeastOnce);
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
