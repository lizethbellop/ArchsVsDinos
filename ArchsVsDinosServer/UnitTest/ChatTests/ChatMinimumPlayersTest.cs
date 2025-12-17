using ArchsVsDinosServer;
using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Services.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts;
using Contracts.DTO;
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
    public class ChatMinimumPlayersTest : BaseTestClass
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
        public void TestLobbyClosesWithOnePlayer()
        {
            string username = "user1";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            SetupMockUserSet(new List<UserAccount> { user });

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = null
            };
            chat.Connect(request);

            chat.Disconnect(username);

            mockCallback.Verify(c => c.LobbyClosedDueToInsufficientPlayers(
                It.IsAny<string>()), Times.Once);
            mockLobbyNotifier.Verify(n => n.NotifyLobbyClosure(
                It.IsAny<string>()), Times.Once);
            Assert.AreEqual(0, GetConnectedUsersCount());
        }

        [TestMethod]
        public void TestLobbyClosesWithTwoPlayersOneDisconnects()
        {
            var mockCallback1 = new Mock<IChatManagerCallback>();
            var mockCallback2 = new Mock<IChatManagerCallback>();

            string username1 = "user1";
            string username2 = "user2";
            UserAccount user1 = new UserAccount { idUser = 1, username = username1 };
            UserAccount user2 = new UserAccount { idUser = 2, username = username2 };

            SetupMockUserSet(new List<UserAccount> { user1, user2 });

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback1.Object);
            ChatConnectionRequest request1 = new ChatConnectionRequest { Username = username1, Context = 0, MatchCode = null };
            chat.Connect(request1);

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback2.Object);
            ChatConnectionRequest request2 = new ChatConnectionRequest { Username = username2, Context = 0, MatchCode = null };
            chat.Connect(request2);

            chat.Disconnect(username1);

            mockCallback2.Verify(c => c.LobbyClosedDueToInsufficientPlayers(
                It.Is<string>(s => s.Contains("Insufficient players"))), Times.Once);
            mockLobbyNotifier.Verify(n => n.NotifyLobbyClosure(
                It.IsAny<string>()), Times.Once);
            Assert.AreEqual(0, GetConnectedUsersCount());
        }

        [TestMethod]
        public void TestLobbyDoesNotCloseWithThreePlayers()
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

            mockCallback.Verify(c => c.LobbyClosedDueToInsufficientPlayers(
                It.IsAny<string>()), Times.Never);
            mockLobbyNotifier.Verify(n => n.NotifyLobbyClosure(
                It.IsAny<string>()), Times.Never);
            Assert.AreEqual(2, GetConnectedUsersCount());
        }

        [TestMethod]
        public void TestGameClosesWithOnePlayer()
        {
            string matchCode = "MATCH123";
            string username = "user1";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            SetupMockUserSet(new List<UserAccount> { user });

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 1,
                MatchCode = matchCode
            };
            chat.Connect(request);

            chat.Disconnect(username);

            mockCallback.Verify(c => c.LobbyClosedDueToInsufficientPlayers(
                It.IsAny<string>()), Times.Once);
            mockGameNotifier.Verify(n => n.NotifyGameClosure(
                matchCode,
                It.IsAny<string>()), Times.Once);
            Assert.AreEqual(0, GetConnectedUsersCount());
        }

        [TestMethod]
        public void TestGameClosesWithTwoPlayersOneDisconnects()
        {
            var mockCallback1 = new Mock<IChatManagerCallback>();
            var mockCallback2 = new Mock<IChatManagerCallback>();

            string matchCode = "MATCH123";
            string username1 = "user1";
            string username2 = "user2";
            UserAccount user1 = new UserAccount { idUser = 1, username = username1 };
            UserAccount user2 = new UserAccount { idUser = 2, username = username2 };

            SetupMockUserSet(new List<UserAccount> { user1, user2 });

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback1.Object);
            ChatConnectionRequest request1 = new ChatConnectionRequest { Username = username1, Context = 1, MatchCode = matchCode };
            chat.Connect(request1);

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback2.Object);
            ChatConnectionRequest request2 = new ChatConnectionRequest { Username = username2, Context = 1, MatchCode = matchCode };
            chat.Connect(request2);

            chat.Disconnect(username1);

            mockCallback2.Verify(c => c.LobbyClosedDueToInsufficientPlayers(
                It.Is<string>(s => s.Contains("Insufficient players"))), Times.Once);
            mockGameNotifier.Verify(n => n.NotifyGameClosure(
                matchCode,
                It.IsAny<string>()), Times.Once);
            Assert.AreEqual(0, GetConnectedUsersCount());
        }

        [TestMethod]
        public void TestGameDoesNotCloseWithThreePlayers()
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

            mockCallback.Verify(c => c.LobbyClosedDueToInsufficientPlayers(
                It.IsAny<string>()), Times.Never);
            mockGameNotifier.Verify(n => n.NotifyGameClosure(
                matchCode,
                It.IsAny<string>()), Times.Never);
            Assert.AreEqual(2, GetConnectedUsersCount());
        }

        [TestMethod]
        public void TestMultipleGamesIndependentClosure()
        {
            var mockCallback1 = new Mock<IChatManagerCallback>();
            var mockCallback2 = new Mock<IChatManagerCallback>();
            var mockCallback3 = new Mock<IChatManagerCallback>();

            string matchCode1 = "MATCH123";
            string matchCode2 = "MATCH456";
            string username1 = "user1";
            string username2 = "user2";
            string username3 = "user3";

            UserAccount user1 = new UserAccount { idUser = 1, username = username1 };
            UserAccount user2 = new UserAccount { idUser = 2, username = username2 };
            UserAccount user3 = new UserAccount { idUser = 3, username = username3 };

            SetupMockUserSet(new List<UserAccount> { user1, user2, user3 });

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback1.Object);
            ChatConnectionRequest request1 = new ChatConnectionRequest { Username = username1, Context = 1, MatchCode = matchCode1 };
            chat.Connect(request1);

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback2.Object);
            ChatConnectionRequest request2 = new ChatConnectionRequest { Username = username2, Context = 1, MatchCode = matchCode1 };
            chat.Connect(request2);

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback3.Object);
            ChatConnectionRequest request3 = new ChatConnectionRequest { Username = username3, Context = 1, MatchCode = matchCode2 };
            chat.Connect(request3);

            chat.Disconnect(username3);

            mockGameNotifier.Verify(n => n.NotifyGameClosure(matchCode2, It.IsAny<string>()), Times.Once);
            mockGameNotifier.Verify(n => n.NotifyGameClosure(matchCode1, It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void TestLobbyAndGameIndependent()
        {
            var mockCallback1 = new Mock<IChatManagerCallback>();
            var mockCallback2 = new Mock<IChatManagerCallback>();

            string matchCode = "MATCH123";
            string username1 = "user1";
            string username2 = "user2";

            UserAccount user1 = new UserAccount { idUser = 1, username = username1 };
            UserAccount user2 = new UserAccount { idUser = 2, username = username2 };

            SetupMockUserSet(new List<UserAccount> { user1, user2 });

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback1.Object);
            ChatConnectionRequest request1 = new ChatConnectionRequest { Username = username1, Context = 0, MatchCode = null };
            chat.Connect(request1);

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback2.Object);
            ChatConnectionRequest request2 = new ChatConnectionRequest { Username = username2, Context = 1, MatchCode = matchCode };
            chat.Connect(request2);

            chat.Disconnect(username2);

            mockGameNotifier.Verify(n => n.NotifyGameClosure(matchCode, It.IsAny<string>()), Times.Once);
            mockLobbyNotifier.Verify(n => n.NotifyLobbyClosure(It.IsAny<string>()), Times.Never);
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
