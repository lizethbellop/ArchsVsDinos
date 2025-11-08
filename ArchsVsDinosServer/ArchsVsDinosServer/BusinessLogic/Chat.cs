using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Services;
using ArchsVsDinosServer.Utils;
using ArchsVsDinosServer.Wrappers;
using Contracts;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic
{

    public class Chat
    {
        private class UserConnection
        {
            public int UserId { get; set; }
            public IChatManagerCallback Callback { get; set; }
        }

        private static readonly ConcurrentDictionary<string, UserConnection> ConnectedUsers = new ConcurrentDictionary<string, UserConnection>();
        private const string DefaultRoom = "Lobby";
        private readonly ILoggerHelper loggerHelper;
        private readonly StrikeManager strikeManager;
        private readonly Func<IDbContext> contextFactory;

        public Chat(ILoggerHelper loggerHelper, Func<IDbContext> contextFactory)
        {
            this.loggerHelper = loggerHelper;
            this.contextFactory = contextFactory;

            var dependencies = new ServiceDependencies(
                new Wrappers.SecurityHelperWrapper(),
                new Wrappers.ValidationHelperWrapper(),
                loggerHelper,
                contextFactory
            );

            const string DATA_FOLDER = "Data";
            const string BANNED_WORDS_FILE = "bannedWords.txt";
            string bannedWordsPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                DATA_FOLDER,
                BANNED_WORDS_FILE
            );

            var profanityFilter = new ProfanityFilter(loggerHelper, bannedWordsPath);
            this.strikeManager = new StrikeManager(dependencies, profanityFilter);
        }

        public Chat(ILoggerHelper loggerHelper)
            : this(loggerHelper, () => new DbContextWrapper())
        {
        }

        public void Connect(string username)
        {
            IChatManagerCallback callback;
            try
            {
                callback = OperationContext.Current.GetCallbackChannel<IChatManagerCallback>();
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"Error obtaining callback: {ex.Message}", ex);
                return;
            }

            int userId = GetUserIdFromUsername(username);
            if (userId == 0)
            {
                try
                {
                    callback.ReceiveSystemNotification(ChatResultCode.Chat_Error, "User not found");
                }
                catch (CommunicationObjectAbortedException ex)
                {
                    loggerHelper.LogWarning($"Communication object aborted for {username}: {ex.Message}");
                }
                catch (CommunicationException ex)
                {
                    loggerHelper.LogWarning($"Communication error sending notification to {username}: {ex.Message}");
                }
                catch (TimeoutException ex)
                {
                    loggerHelper.LogWarning($"Timeout sending notification to {username}: {ex.Message}");
                }
                catch (ObjectDisposedException ex)
                {
                    loggerHelper.LogWarning($"Object disposed for {username}: {ex.Message}");
                }
                return;
            }

            var userConnection = new UserConnection
            {
                UserId = userId,
                Callback = callback
            };

            if (!ConnectedUsers.TryAdd(username, userConnection))
            {
                try
                {
                    callback.ReceiveSystemNotification(ChatResultCode.Chat_UserAlreadyConnected, "User already connected");
                }
                catch (CommunicationObjectAbortedException ex)
                {
                    loggerHelper.LogWarning($"Communication object aborted for {username}: {ex.Message}");
                }
                catch (CommunicationException ex)
                {
                    loggerHelper.LogWarning($"Communication error sending notification to {username}: {ex.Message}");
                }
                catch (TimeoutException ex)
                {
                    loggerHelper.LogWarning($"Timeout sending notification to {username}: {ex.Message}");
                }
                catch (ObjectDisposedException ex)
                {
                    loggerHelper.LogWarning($"Object disposed for {username}: {ex.Message}");
                }
                return;
            }

            BroadcastSystemNotificationWithEnum(ChatResultCode.Chat_UserConnected, $"{username} has joined");
            UpdateUserList();
        }

        public void Disconnect(string username)
        {
            if (ConnectedUsers.TryRemove(username, out _))
            {
                BroadcastSystemNotificationWithEnum(ChatResultCode.Chat_UserDisconnected, $"{username} has left the lobby");
                UpdateUserList();
            }
        }

        public void SendMessageToRoom(string message, string username)
        {
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(username))
            {
                return;
            }

            try
            {
                if (!ConnectedUsers.TryGetValue(username, out var userConnection))
                {
                    loggerHelper.LogWarning($"User {username} not found in connected users");
                    return;
                }

                bool canSend = strikeManager.CanSendMessage(userConnection.UserId, message);

                if (!canSend)
                {
                    NotifyUserMessageBlocked(username);
                    loggerHelper.LogInfo($"Message from {username} blocked due to profanity or ban");
                    return;
                }

                BroadcastMessageToAll(username, message);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error sending message from {username}: {ex.Message}", ex);
            }
        }

        private int GetUserIdFromUsername(string username)
        {
            try
            {
                using (var context = contextFactory())
                {
                    var user = context.UserAccount.FirstOrDefault(u => u.username == username);
                    return user?.idUser ?? 0;
                }
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error getting userId for username {username}", ex);
                return 0;
            }
        }

        private void NotifyUserMessageBlocked(string username)
        {
            if (ConnectedUsers.TryGetValue(username, out var userConnection))
            {
                SafeCallbackInvoke(username, () =>
                {
                    userConnection.Callback.ReceiveSystemNotification(
                        ChatResultCode.Chat_MessageBlocked,
                        "Your message contains inappropriate language and was blocked"
                    );
                });
            }
        }

        private void BroadcastSystemNotificationWithEnum(ChatResultCode code, string message)
        {
            foreach (var user in ConnectedUsers.ToArray())
            {
                SafeCallbackInvoke(user.Key, () =>
                {
                    user.Value.Callback.ReceiveSystemNotification(code, message);
                });
            }
        }

        private void BroadcastMessageToAll(string fromUser, string message)
        {
            foreach (var user in ConnectedUsers.ToArray())
            {
                SafeCallbackInvoke(user.Key, () =>
                {
                    user.Value.Callback.ReceiveMessage(DefaultRoom, fromUser, message);
                });
            }
        }

        private void UpdateUserList()
        {
            var users = ConnectedUsers.Keys.ToList();

            foreach (var user in ConnectedUsers.ToArray())
            {
                SafeCallbackInvoke(user.Key, () =>
                {
                    user.Value.Callback.UpdateUserList(users);
                });
            }
        }

        private void SafeCallbackInvoke(string username, Action callbackAction)
        {
            try
            {
                callbackAction();
            }
            catch (CommunicationObjectAbortedException)
            {
                ConnectedUsers.TryRemove(username, out _);
            }
            catch (CommunicationException)
            {
                loggerHelper.LogWarning($"Communication error with user {username}");
                ConnectedUsers.TryRemove(username, out _);
            }
            catch (TimeoutException)
            {
                loggerHelper.LogWarning($"Timeout communicating with user {username}");
                ConnectedUsers.TryRemove(username, out _);
            }
            catch (ObjectDisposedException)
            {
                ConnectedUsers.TryRemove(username, out _);
            }
        }
    }
}
