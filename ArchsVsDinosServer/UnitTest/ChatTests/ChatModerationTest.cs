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
    public class ChatModerationTest : BaseTestClass
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
            // IMPORTANTE: Limpiar ANTES de BaseSetup
            connectedUsersField = typeof(Chat).GetField("ConnectedUsers",
                BindingFlags.NonPublic | BindingFlags.Static);
            ClearConnectedUsers();

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
        }

        [TestCleanup]
        public void Cleanup()
        {
            ClearConnectedUsers();
        }

        [TestMethod]
        public void TestFirstStrikeNotifiesUser()
        {
            string username = "user1";
            string badMessage = "badword";
            string lobbyCode = "LOBBY123";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            SetupMockUserSet(new List<UserAccount> { user });

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = false,
                    ShouldBan = false,
                    CurrentStrikes = 1,
                    Reason = "Warning 1/3"
                });

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = lobbyCode
            };
            chat.Connect(request);

            chat.SendMessageToRoom(badMessage, username);

            mockCallback.Verify(c => c.ReceiveSystemNotification(
                ChatResultCode.Chat_MessageBlocked,
                It.Is<string>(s => s.Contains("Warning 1/3"))), Times.Once);
        }

        [TestMethod]
        public void TestFirstStrikeBlocksMessage()
        {
            string username = "user1";
            string badMessage = "badword";
            string lobbyCode = "LOBBY123";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            SetupMockUserSet(new List<UserAccount> { user });

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = false,
                    ShouldBan = false,
                    CurrentStrikes = 1,
                    Reason = "Warning 1/3"
                });

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = lobbyCode
            };
            chat.Connect(request);

            chat.SendMessageToRoom(badMessage, username);

            mockCallback.Verify(c => c.ReceiveMessage(
                It.IsAny<string>(),
                It.IsAny<string>(),
                badMessage), Times.Never);
        }

        [TestMethod]
        public void TestSecondStrikeNotifiesUser()
        {
            string username = "user1";
            string badMessage = "badword";
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

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = false,
                    ShouldBan = false,
                    CurrentStrikes = 1,
                    Reason = "Warning 1/3"
                });
            chat.SendMessageToRoom(badMessage, username);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = false,
                    ShouldBan = false,
                    CurrentStrikes = 2,
                    Reason = "Warning 2/3"
                });
            chat.SendMessageToRoom(badMessage, username);

            mockCallback.Verify(c => c.ReceiveSystemNotification(
                ChatResultCode.Chat_MessageBlocked,
                It.Is<string>(s => s.Contains("Warning 2/3"))), Times.Once);
        }

        [TestMethod]
        public void TestThirdStrikeInLobbyCallsUserBanned()
        {
            string username = "user1";
            string lobbyCode = "LOBBY123";
            string badMessage = "badword";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            SetupMockUserSet(new List<UserAccount> { user });

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = lobbyCode
            };
            chat.Connect(request);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = false,
                    ShouldBan = false,
                    CurrentStrikes = 1,
                    Reason = "Warning 1/3"
                });
            chat.SendMessageToRoom(badMessage, username);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = false,
                    ShouldBan = false,
                    CurrentStrikes = 2,
                    Reason = "Warning 2/3"
                });
            chat.SendMessageToRoom(badMessage, username);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = false,
                    ShouldBan = true,
                    CurrentStrikes = 3,
                    Reason = "User expelled due to repeated inappropriate messages"
                });
            chat.SendMessageToRoom(badMessage, username);

            mockCallback.Verify(c => c.UserBannedFromChat(username, 3), Times.Once);
        }

        [TestMethod]
        public void TestThirdStrikeInLobbyNotifiesLobbyService()
        {
            string username = "user1";
            string lobbyCode = "LOBBY123";
            string badMessage = "badword";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            SetupMockUserSet(new List<UserAccount> { user });

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = lobbyCode
            };
            chat.Connect(request);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = false,
                    ShouldBan = false,
                    CurrentStrikes = 1,
                    Reason = "Warning 1/3"
                });
            chat.SendMessageToRoom(badMessage, username);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = false,
                    ShouldBan = false,
                    CurrentStrikes = 2,
                    Reason = "Warning 2/3"
                });
            chat.SendMessageToRoom(badMessage, username);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = false,
                    ShouldBan = true,
                    CurrentStrikes = 3,
                    Reason = "User expelled due to repeated inappropriate messages"
                });
            chat.SendMessageToRoom(badMessage, username);

            mockLobbyNotifier.Verify(n => n.NotifyPlayerExpelled(lobbyCode, user.idUser, "Inappropriate language"), Times.Once);
        }

        [TestMethod]
        public void TestThirdStrikeInLobbyRemovesUser()
        {
            string username = "user1";
            string lobbyCode = "LOBBY123";
            string badMessage = "badword";
            UserAccount user = new UserAccount { idUser = 1, username = username };

            SetupMockUserSet(new List<UserAccount> { user });

            ChatConnectionRequest request = new ChatConnectionRequest
            {
                Username = username,
                Context = 0,
                MatchCode = lobbyCode
            };
            chat.Connect(request);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult { CanSendMessage = false, ShouldBan = false, CurrentStrikes = 1 });
            chat.SendMessageToRoom(badMessage, username);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult { CanSendMessage = false, ShouldBan = false, CurrentStrikes = 2 });
            chat.SendMessageToRoom(badMessage, username);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult { CanSendMessage = false, ShouldBan = true, CurrentStrikes = 3 });
            chat.SendMessageToRoom(badMessage, username);

            Assert.AreEqual(0, GetConnectedUsersCount());
        }

        [TestMethod]
        public void TestThirdStrikeInGameCallsUserBanned()
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

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = false,
                    ShouldBan = false,
                    CurrentStrikes = 1,
                    Reason = "Warning 1/3"
                });
            chat.SendMessageToRoom(badMessage, username);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = false,
                    ShouldBan = false,
                    CurrentStrikes = 2,
                    Reason = "Warning 2/3"
                });
            chat.SendMessageToRoom(badMessage, username);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = false,
                    ShouldBan = true,
                    CurrentStrikes = 3,
                    Reason = "User expelled due to repeated inappropriate messages"
                });
            chat.SendMessageToRoom(badMessage, username);

            mockCallback.Verify(c => c.UserBannedFromChat(username, 3), Times.Once);
        }

        [TestMethod]
        public void TestThirdStrikeInGameNotifiesGameService()
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

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = false,
                    ShouldBan = false,
                    CurrentStrikes = 1,
                    Reason = "Warning 1/3"
                });
            chat.SendMessageToRoom(badMessage, username);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = false,
                    ShouldBan = false,
                    CurrentStrikes = 2,
                    Reason = "Warning 2/3"
                });
            chat.SendMessageToRoom(badMessage, username);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = false,
                    ShouldBan = true,
                    CurrentStrikes = 3,
                    Reason = "User expelled due to repeated inappropriate messages"
                });
            chat.SendMessageToRoom(badMessage, username);

            mockGameNotifier.Verify(n => n.NotifyPlayerExpelled(matchCode, user.idUser, "Inappropriate language"), Times.Once);
        }

        [TestMethod]
        public void TestThirdStrikeInGameRemovesUser()
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

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult { CanSendMessage = false, ShouldBan = false, CurrentStrikes = 1 });
            chat.SendMessageToRoom(badMessage, username);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult { CanSendMessage = false, ShouldBan = false, CurrentStrikes = 2 });
            chat.SendMessageToRoom(badMessage, username);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult { CanSendMessage = false, ShouldBan = true, CurrentStrikes = 3 });
            chat.SendMessageToRoom(badMessage, username);

            Assert.AreEqual(0, GetConnectedUsersCount());
        }

        [TestMethod]
        public void TestBanBroadcastsSystemNotification()
        {
            var mockCallback1 = new Mock<IChatManagerCallback>();
            var mockCallback2 = new Mock<IChatManagerCallback>();

            string username1 = "user1";
            string username2 = "user2";
            string badMessage = "badword";
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

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = false,
                    ShouldBan = false,
                    CurrentStrikes = 1,
                    Reason = "Warning 1/3"
                });
            chat.SendMessageToRoom(badMessage, username1);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = false,
                    ShouldBan = false,
                    CurrentStrikes = 2,
                    Reason = "Warning 2/3"
                });
            chat.SendMessageToRoom(badMessage, username1);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult
                {
                    CanSendMessage = false,
                    ShouldBan = true,
                    CurrentStrikes = 3,
                    Reason = "User expelled due to repeated inappropriate messages"
                });
            chat.SendMessageToRoom(badMessage, username1);

            mockCallback2.Verify(c => c.ReceiveSystemNotification(
                ChatResultCode.Chat_UserBanned,
                It.Is<string>(s => s.Contains(username1) && s.Contains("expelled"))), Times.Once);
        }

        [TestMethod]
        public void TestBannedUserCannotSendMessages()
        {
            string username = "user1";
            string badMessage = "badword";
            string normalMessage = "Hello";
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

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult { CanSendMessage = false, ShouldBan = false, CurrentStrikes = 1 });
            chat.SendMessageToRoom(badMessage, username);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult { CanSendMessage = false, ShouldBan = false, CurrentStrikes = 2 });
            chat.SendMessageToRoom(badMessage, username);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult { CanSendMessage = false, ShouldBan = true, CurrentStrikes = 3 });
            chat.SendMessageToRoom(badMessage, username);

            chat.SendMessageToRoom(normalMessage, username);

            mockCallback.Verify(c => c.ReceiveMessage(
                It.IsAny<string>(),
                It.IsAny<string>(),
                normalMessage), Times.Never);
        }

        [TestMethod]
        public void TestBanInLobbyTriggersMinimumPlayersCheck()
        {
            var mockCallback1 = new Mock<IChatManagerCallback>();
            var mockCallback2 = new Mock<IChatManagerCallback>();

            string username1 = "user1";
            string username2 = "user2";
            string lobbyCode = "LOBBY123";
            string badMessage = "badword";

            UserAccount user1 = new UserAccount { idUser = 1, username = username1 };
            UserAccount user2 = new UserAccount { idUser = 2, username = username2 };

            SetupMockUserSet(new List<UserAccount> { user1, user2 });

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback1.Object);
            ChatConnectionRequest request1 = new ChatConnectionRequest { Username = username1, Context = 0, MatchCode = lobbyCode };
            chat.Connect(request1);

            mockCallbackProvider.Setup(p => p.GetCallback()).Returns(mockCallback2.Object);
            ChatConnectionRequest request2 = new ChatConnectionRequest { Username = username2, Context = 0, MatchCode = lobbyCode };
            chat.Connect(request2);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult { CanSendMessage = false, ShouldBan = false, CurrentStrikes = 1 });
            chat.SendMessageToRoom(badMessage, username1);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult { CanSendMessage = false, ShouldBan = false, CurrentStrikes = 2 });
            chat.SendMessageToRoom(badMessage, username1);

            mockModerationManager.Setup(m => m.ModerateMessage(It.IsAny<ModerationRequestDTO>()))
                .Returns(new ModerationResult { CanSendMessage = false, ShouldBan = true, CurrentStrikes = 3 });
            chat.SendMessageToRoom(badMessage, username1);

            // Verificar que se expulsó al jugador
            mockLobbyNotifier.Verify(n => n.NotifyPlayerExpelled(lobbyCode, user1.idUser, "Inappropriate language"), Times.Once);

            // Verificar que quedó solo 1 jugador en el lobby después del ban
            Assert.AreEqual(1, GetConnectedUsersCount());

            // Verificar que el logger registró la advertencia de jugadores insuficientes
            mockLoggerHelper.Verify(l => l.LogWarning(
                It.Is<string>(s => s.Contains("Low players in lobby") && s.Contains(lobbyCode))),
                Times.Once);
        }

        private void ClearConnectedUsers()
        {
            if (connectedUsersField == null)
            {
                connectedUsersField = typeof(Chat).GetField("ConnectedUsers",
                    BindingFlags.NonPublic | BindingFlags.Static);
            }

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
            if (connectedUsersField == null)
            {
                connectedUsersField = typeof(Chat).GetField("ConnectedUsers",
                    BindingFlags.NonPublic | BindingFlags.Static);
            }

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
