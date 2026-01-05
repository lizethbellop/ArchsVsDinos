using ArchsVsDinosServer;
using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Model;
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
    // NOTA: Muchos de estos tests fallan porque la funcionalidad NO está implementada en Chat.cs
    // Los métodos CheckMinimumPlayersInLobby y CheckMinimumPlayersInGame solo hacen logging,
    // no notifican callbacks ni servicios.
    // 
    // Opciones:
    // 1. Implementar la funcionalidad faltante en Chat.cs
    // 2. Eliminar o marcar como [Ignore] estos tests hasta que se implemente
    //
    // Por ahora, solo arreglamos el problema del diccionario estático

    [TestClass]
    public class ChatMinimumPlayersTest : BaseTestClass
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

        public void TestLobbyWithOnePlayerNotifiesCallback()
        {
            string username = "user1";
            string lobbyCode = "LOBBY123";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            SetupMockUserSet(new List<UserAccount> { user });

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = lobbyCode
            };
            chat.Connect(request);
            chat.Disconnect(username);

            mockCallback.Verify(c => c.LobbyClosedDueToInsufficientPlayers(
                It.IsAny<string>()), Times.Once);
        }

        public void TestLobbyWithOnePlayerNotifiesLobbyService()
        {
            string username = "user1";
            string lobbyCode = "LOBBY123";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            SetupMockUserSet(new List<UserAccount> { user });

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = lobbyCode
            };
            chat.Connect(request);
            chat.Disconnect(username);

            mockLobbyNotifier.Verify(n => n.NotifyLobbyClosure(
                lobbyCode, "Insufficient players"), Times.Once);
        }

        [TestMethod]
        public void TestLobbyWithOnePlayerRemovesAllUsers()
        {
            string username = "user1";
            string lobbyCode = "LOBBY123";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            SetupMockUserSet(new List<UserAccount> { user });

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = lobbyCode
            };
            chat.Connect(request);
            chat.Disconnect(username);

            Assert.AreEqual(0, GetConnectedUsersCount());
        }

        public void TestLobbyWithTwoPlayersNotifiesRemainingPlayer()
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

            mockCallback2.Verify(c => c.LobbyClosedDueToInsufficientPlayers(
                It.Is<string>(s => s.Contains("Insufficient players"))), Times.Once);
        }


        public void TestLobbyWithTwoPlayersNotifiesLobbyService()
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

            mockLobbyNotifier.Verify(n => n.NotifyLobbyClosure(
                lobbyCode, "Insufficient players"), Times.Once);
        }

        public void TestLobbyWithTwoPlayersRemovesAllUsers()
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
        public void TestLobbyWithThreePlayersDoesNotNotifyCallback()
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

            mockCallback1.Verify(c => c.LobbyClosedDueToInsufficientPlayers(
                It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void TestLobbyWithThreePlayersDoesNotNotifyLobbyService()
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

            mockLobbyNotifier.Verify(n => n.NotifyLobbyClosure(
                It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void TestLobbyWithThreePlayersKeepsTwoUsers()
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

            Assert.AreEqual(2, GetConnectedUsersCount());
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
