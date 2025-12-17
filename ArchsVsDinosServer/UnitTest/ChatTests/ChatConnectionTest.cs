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

        /*private Mock<ILobbyNotifier> mockLobbyNotifier;
        private Mock<IGameServiceNotifier> mockGameNotifier;
        private Mock<ICallbackProvider> mockCallbackProvider;
        private Mock<IChatManagerCallback> mockCallback;
        private Chat chat;
        private FieldInfo connectedUsersField;

        [TestInitialize]
        public void Setup()
        {
            base.BaseSetup();

            mockLobbyNotifier = new Mock<ILobbyNotifier>();
            mockGameNotifier = new Mock<IGameServiceNotifier>();
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
        public void TestConnectUserNotFound()
        {
            string username = "nonexistent";
            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = null
            };

            SetupMockUserSet(new List<UserAccount>());

            chat.Connect(request);

            mockCallback.Verify(c => c.ReceiveSystemNotification(
                ChatResultCode.Chat_Error,
                "User not found"), Times.Once);

            Assert.AreEqual(0, GetConnectedUsersCount());
        }

        [TestMethod]
        public void TestConnectSuccessInLobby()
        {
            string username = "user1";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = null
            };

            SetupMockUserSet(new List<UserAccount> { user });

            chat.Connect(request);

            Assert.AreEqual(1, GetConnectedUsersCount());
            mockCallback.Verify(c => c.ReceiveSystemNotification(
                ChatResultCode.Chat_UserConnected,
                It.Is<string>(s => s.Contains(username))), Times.Once);
        }

        [TestMethod]
        public void TestConnectSuccessInGame()
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
        public void TestConnectUserAlreadyConnected()
        {
            string username = "user1";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = null
            };

            SetupMockUserSet(new List<UserAccount> { user });

            chat.Connect(request);
            chat.Connect(request);

            mockCallback.Verify(c => c.ReceiveSystemNotification(
                ChatResultCode.Chat_UserAlreadyConnected,
                "User already connected"), Times.Once);

            Assert.AreEqual(1, GetConnectedUsersCount());
        }

        [TestMethod]
        public void TestConnectCallbackError()
        {
            string username = "user1";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = null
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
                MatchCode = null
            };

            SetupMockUserSet(new List<UserAccount> { user });

            chat.Connect(request);

            mockCallback.Verify(c => c.UpdateUserList(
                It.Is<List<string>>(list => list.Contains(username))), Times.AtLeastOnce);
        }

        [TestMethod]
        public void TestDisconnectSuccess()
        {
            string username = "user1";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = null
            };

            SetupMockUserSet(new List<UserAccount> { user });

            chat.Connect(request);
            Assert.AreEqual(1, GetConnectedUsersCount());

            chat.Disconnect(username);
            Assert.AreEqual(0, GetConnectedUsersCount());
        }

        [TestMethod]
        public void TestDisconnectNonExistentUser()
        {
            string username = "nonexistent";

            chat.Disconnect(username);

            Assert.AreEqual(0, GetConnectedUsersCount());
        }

        [TestMethod]
        public void TestDisconnectUpdatesUserList()
        {
            string username1 = "user1";
            string username2 = "user2";
            UserAccount user1 = new UserAccount { idUser = 1, username = username1 };
            UserAccount user2 = new UserAccount { idUser = 2, username = username2 };

            SetupMockUserSet(new List<UserAccount> { user1, user2 });

            ChatConnectionRequest request1 = new ChatConnectionRequest
            {
                Username = username1,
                Context = 0,
                MatchCode = null
            };
            chat.Connect(request1);

            ChatConnectionRequest request2 = new ChatConnectionRequest
            {
                Username = username2,
                Context = 0,
                MatchCode = null
            };
            chat.Connect(request2);

            chat.Disconnect(username1);

            mockCallback.Verify(c => c.UpdateUserList(
                It.Is<List<string>>(list => !list.Contains(username1))), Times.AtLeastOnce);
        }

        [TestMethod]
        public void TestDisconnectFromLobbyWithTwoUsers()
        {
            string username1 = "user1";
            string username2 = "user2";
            UserAccount user1 = new UserAccount { idUser = 1, username = username1 };
            UserAccount user2 = new UserAccount { idUser = 2, username = username2 };

            SetupMockUserSet(new List<UserAccount> { user1, user2 });

            ChatConnectionRequest request1 = new ChatConnectionRequest
            {
                Username = username1,
                Context = 0,
                MatchCode = null
            };
            chat.Connect(request1);

            ChatConnectionRequest request2 = new ChatConnectionRequest
            {
                Username = username2,
                Context = 0,
                MatchCode = null
            };
            chat.Connect(request2);

            chat.Disconnect(username1);

            mockLobbyNotifier.Verify(n => n.NotifyLobbyClosure(
                It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void TestDisconnectFromLobbyWithThreeUsersNoNotification()
        {
            string username1 = "user1";
            string username2 = "user2";
            string username3 = "user3";
            UserAccount user1 = new UserAccount { idUser = 1, username = username1 };
            UserAccount user2 = new UserAccount { idUser = 2, username = username2 };
            UserAccount user3 = new UserAccount { idUser = 3, username = username3 };

            SetupMockUserSet(new List<UserAccount> { user1, user2, user3 });

            ChatConnectionRequest request1 = new ChatConnectionRequest { Username = username1, Context = 0, MatchCode = null };
            ChatConnectionRequest request2 = new ChatConnectionRequest { Username = username2, Context = 0, MatchCode = null };
            ChatConnectionRequest request3 = new ChatConnectionRequest { Username = username3, Context = 0, MatchCode = null };

            chat.Connect(request1);
            chat.Connect(request2);
            chat.Connect(request3);

            chat.Disconnect(username1);

            mockLobbyNotifier.Verify(n => n.NotifyLobbyClosure(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void TestDisconnectFromGameWithTwoUsers()
        {
            string matchCode = "MATCH123";
            string username1 = "user1";
            string username2 = "user2";
            UserAccount user1 = new UserAccount { idUser = 1, username = username1 };
            UserAccount user2 = new UserAccount { idUser = 2, username = username2 };

            SetupMockUserSet(new List<UserAccount> { user1, user2 });

            ChatConnectionRequest request1 = new ChatConnectionRequest
            {
                Username = username1,
                Context = 1,
                MatchCode = matchCode
            };
            chat.Connect(request1);

            ChatConnectionRequest request2 = new ChatConnectionRequest
            {
                Username = username2,
                Context = 1,
                MatchCode = matchCode
            };
            chat.Connect(request2);

            chat.Disconnect(username1);

            mockGameNotifier.Verify(n => n.NotifyGameClosure(
                matchCode,
                It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void TestDisconnectFromGameWithThreeUsersNoNotification()
        {
            string matchCode = "MATCH123";
            string username1 = "user1";
            string username2 = "user2";
            string username3 = "user3";
            UserAccount user1 = new UserAccount { idUser = 1, username = username1 };
            UserAccount user2 = new UserAccount { idUser = 2, username = username2 };
            UserAccount user3 = new UserAccount { idUser = 3, username = username3 };

            SetupMockUserSet(new List<UserAccount> { user1, user2, user3 });

            ChatConnectionRequest request1 = new ChatConnectionRequest { Username = username1, Context = 1, MatchCode = matchCode };
            ChatConnectionRequest request2 = new ChatConnectionRequest { Username = username2, Context = 1, MatchCode = matchCode };
            ChatConnectionRequest request3 = new ChatConnectionRequest { Username = username3, Context = 1, MatchCode = matchCode };

            chat.Connect(request1);
            chat.Connect(request2);
            chat.Connect(request3);

            chat.Disconnect(username1);

            mockGameNotifier.Verify(n => n.NotifyGameClosure(matchCode, It.IsAny<string>()), Times.Never);
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
        }*/
    }
}
