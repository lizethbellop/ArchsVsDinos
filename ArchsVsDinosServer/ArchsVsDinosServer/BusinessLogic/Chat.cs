using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Model;
using ArchsVsDinosServer.Services;
using ArchsVsDinosServer.Services.Interfaces;
using ArchsVsDinosServer.Utils;
using ArchsVsDinosServer.Wrappers;
using Contracts;
using Contracts.DTO;
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
            public int Context { get; set; }
            public string MatchCode { get; set; }
        }

        private static readonly ConcurrentDictionary<string, UserConnection> ConnectedUsers = new ConcurrentDictionary<string, UserConnection>();
        private const string DefaultRoom = "Lobby";
        private const int MinimumPlayersRequired = 2;
        private const int ContextLobby = 0;
        private const int ContextGame = 1;
        private readonly ILoggerHelper loggerHelper;
        private readonly Func<IDbContext> contextFactory;
        private readonly ILobbyServiceNotifier lobbyNotifier;
        private readonly IGameServiceNotifier gameNotifier;
        private readonly ICallbackProvider callbackProvider;
        private readonly IModerationManager moderationManager;

        public Chat(
            ChatServiceDependencies dependencies,
            ILobbyServiceNotifier lobbyNotifier,
            IGameServiceNotifier gameNotifier)
        {
            this.loggerHelper = dependencies.LoggerHelper;
            this.contextFactory = dependencies.ContextFactory;
            this.lobbyNotifier = lobbyNotifier;
            this.gameNotifier = gameNotifier;
            this.callbackProvider = dependencies.CallbackProvider;
            this.moderationManager = dependencies.ModerationManager;
        }

        public void Connect(ChatConnectionRequest request)
        {
            IChatManagerCallback callback;
            try
            {
                callback = callbackProvider.GetCallback();
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"Error obtaining callback: {ex.Message}", ex);
                return;
            }

            int userId = GetUserIdFromUsername(request.Username);

            if (userId == 0)
            {
                userId = -Math.Abs(request.Username.GetHashCode());
                loggerHelper.LogInfo($"Guest '{request.Username}' connecting to chat with temporary userId: {userId}");
            }

            var context = request.Context;

            var userConnection = new UserConnection
            {
                UserId = userId,
                Callback = callback,
                Context = context,
                MatchCode = request.MatchCode
            };

            if (!ConnectedUsers.TryAdd(request.Username, userConnection))
            {
                SafeCallbackInvoke(request.Username, () =>
                {
                    callback.ReceiveSystemNotification(ChatResultCode.Chat_UserAlreadyConnected, "User already connected");
                });
                return;
            }

            var existingUsersInMatch = ConnectedUsers
                .Where(u => u.Value.MatchCode == request.MatchCode)
                .Select(u => u.Key)
                .ToList();

            SafeCallbackInvoke(request.Username, () =>
            {
                callback.UpdateUserList(existingUsersInMatch);
            });

            BroadcastSystemNotificationToMatch(
                ChatResultCode.Chat_UserConnected,
                $"{request.Username} has joined",
                request.MatchCode
            );

            UpdateUserListForMatch(request.MatchCode);
        }

        private void UpdateUserListForMatch(string matchCode)
        {
            if (string.IsNullOrEmpty(matchCode)) return;

            var usersInMatch = ConnectedUsers
                .Where(u => u.Value.MatchCode == matchCode)
                .Select(u => u.Key)
                .ToList();

            var targetUsers = ConnectedUsers
                .Where(u => u.Value.MatchCode == matchCode)
                .ToArray();

            foreach (var user in targetUsers)
            {
                SafeCallbackInvoke(user.Key, () =>
                {
                    user.Value.Callback.UpdateUserList(usersInMatch);
                });
            }
        }

        public void Disconnect(string username)
        {
            if (ConnectedUsers.TryRemove(username, out var userConnection))
            {
                BroadcastSystemNotificationToMatch(
                    ChatResultCode.Chat_UserDisconnected,
                    $"{username} has left",
                    userConnection.MatchCode
                );

                UpdateUserListForMatch(userConnection.MatchCode);

                if (userConnection.Context == ContextLobby)
                {
                    CheckMinimumPlayersInLobby(userConnection.MatchCode);
                }
                else if (userConnection.Context == ContextGame)
                {
                    CheckMinimumPlayersInGame(userConnection.MatchCode);
                }
            }
        }

        public void SendMessageToRoom(string message, string username)
        {
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(username))
                return;

            if (!ConnectedUsers.TryGetValue(username, out var user))
            {
                loggerHelper.LogWarning($"User {username} not found");
                return;
            }

            var moderationRequest = new ModerationRequestDTO
            {
                UserId = user.UserId,
                Username = username,
                Message = message,
                Context = user.Context,
                MatchCode = user.MatchCode
            };

            var result = moderationManager.ModerateMessage(moderationRequest);

            if (!result.CanSendMessage)
            {
                if (result.ShouldBan)
                {
                    HandleUserBan(username, result.CurrentStrikes, user.Context, user.MatchCode);
                }
                else
                {
                    NotifyUserMessageBlocked(username, result.CurrentStrikes);
                }

                return;
            }

            BroadcastMessageToMatch(username, message, user.MatchCode);
        }

        private void HandleUserBan(string username, int strikes, int context, string matchCode)
        {
            loggerHelper.LogWarning($"User {username} banned with {strikes} strikes");

            if (ConnectedUsers.TryGetValue(username, out var user))
            {
                SafeCallbackInvoke(username, () =>
                {
                    user.Callback.UserBannedFromChat(username, strikes);
                });
            }

            BroadcastSystemNotificationToMatch(
                ChatResultCode.Chat_UserBanned,
                $"{username} has been expelled for inappropriate behavior",
                matchCode
            );

            if (context == ContextLobby)
                lobbyNotifier?.NotifyPlayerExpelled(matchCode, user.UserId, "Inappropriate language");
            else
                gameNotifier?.NotifyPlayerExpelled(matchCode, user.UserId, "Inappropriate language");

            ConnectedUsers.TryRemove(username, out _);
            UpdateUserList();

            if (context == ContextLobby)
                CheckMinimumPlayersInLobby(matchCode);
            else
                CheckMinimumPlayersInGame(matchCode);
        }

        private void CheckMinimumPlayersInLobby(string lobbyCode)
        {
            int lobbyPlayers = ConnectedUsers.Count(u =>
                u.Value.Context == ContextLobby &&
                u.Value.MatchCode == lobbyCode);

            if (lobbyPlayers < MinimumPlayersRequired)
            {
                loggerHelper.LogWarning($"[CHAT MONITOR] Low players in lobby {lobbyCode}: {lobbyPlayers}. Waiting for LobbyLogic decision.");

                
            }
        }

        private void CheckMinimumPlayersInGame(string matchCode)
        {
            int gamePlayers = ConnectedUsers.Count(u =>
                u.Value.Context == ContextGame &&
                u.Value.MatchCode == matchCode);

            if (gamePlayers < MinimumPlayersRequired)
            {
                loggerHelper.LogWarning($"[CHAT MONITOR] Low players in game {matchCode}: {gamePlayers}.");
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

        private void NotifyUserMessageBlocked(string username, int currentStrikes)
        {
            if (ConnectedUsers.TryGetValue(username, out var userConnection))
            {
                SafeCallbackInvoke(username, () =>
                {
                    userConnection.Callback.ReceiveSystemNotification(
                        ChatResultCode.Chat_MessageBlocked,
                        $"Warning {currentStrikes}/3. At 3 warnings you will be expelled."
                    );
                });
            }
        }

        private void BroadcastSystemNotificationToMatch(ChatResultCode code, string message, string matchCode)
        {
            if (string.IsNullOrEmpty(matchCode)) return;

            var targetUsers = ConnectedUsers
                .Where(u => u.Value.MatchCode == matchCode)
                .ToArray();

            foreach (var user in targetUsers)
            {
                SafeCallbackInvoke(user.Key, () =>
                {
                    user.Value.Callback.ReceiveSystemNotification(code, message);
                });
            }
        }

        private void BroadcastMessageToMatch(string fromUser, string message, string matchCode)
        {
            if (string.IsNullOrEmpty(matchCode)) return;

            var targetUsers = ConnectedUsers
                .Where(u => u.Value.MatchCode == matchCode)
                .ToArray();

            foreach (var user in targetUsers)
            {
                SafeCallbackInvoke(user.Key, () =>
                {
                    user.Value.Callback.ReceiveMessage(DefaultRoom, fromUser, message);
                });
            }
        }

        private void UpdateUserList()
        {
            var groupedUsers = ConnectedUsers
                .GroupBy(u => u.Value.MatchCode)
                .ToList();

            foreach (var group in groupedUsers)
            {
                var usersInThisGroup = group.Select(u => u.Key).ToList();

                foreach (var user in group)
                {
                    SafeCallbackInvoke(user.Key, () =>
                    {
                        user.Value.Callback.UpdateUserList(usersInThisGroup);
                    });
                }
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
            catch (CommunicationException ex)
            {
                loggerHelper.LogWarning($"Communication error with user {username}: {ex.Message}");
                ConnectedUsers.TryRemove(username, out _);
            }
            catch (TimeoutException ex)
            {
                loggerHelper.LogWarning($"Timeout communicating with user {username}: {ex.Message}");
                ConnectedUsers.TryRemove(username, out _);
            }
            catch (ObjectDisposedException)
            {
                ConnectedUsers.TryRemove(username, out _);
            }
        }
    }

}
