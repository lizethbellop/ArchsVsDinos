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
            if (!ValidateConnectionRequest(request))
                return;

            IChatManagerCallback callback = GetCallback(request.Username);
            if (callback == null)
                return;

            if (!ValidateUserId(request))
                return;

            HandleUserReconnection(request.Username, request.MatchCode);

            var userConnection = CreateUserConnection(request, callback);

            if (!TryAddUser(request.Username, userConnection, callback))
                return;

            NotifySuccessfulConnection(request);
        }

        private bool ValidateConnectionRequest(ChatConnectionRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.MatchCode))
            {
                loggerHelper.LogWarning("[CHAT] Invalid connection request");
                return false;
            }
            return true;
        }

        private IChatManagerCallback GetCallback(string username)
        {
            try
            {
                return callbackProvider.GetCallback();
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"[CHAT] Error obtaining callback for {username}: {ex.Message}", ex);
                return null;
            }
        }

        private bool ValidateUserId(ChatConnectionRequest request)
        {
            if (request.UserId == 0)
            {
                loggerHelper.LogWarning(
                    $"[CHAT] Connection rejected: invalid UserId for {request.Username}");
                return false;
            }
            return true;
        }

        private void HandleUserReconnection(string username, string newMatchCode)
        {
            if (ConnectedUsers.TryGetValue(username, out var existingConnection))
            {
                if (existingConnection.MatchCode != newMatchCode)
                {
                    loggerHelper.LogInfo(
                        $"[CHAT] User {username} reconnecting from {existingConnection.MatchCode} to {newMatchCode}");
                    Disconnect(username);
                }
            }
        }

        private UserConnection CreateUserConnection(ChatConnectionRequest request, IChatManagerCallback callback)
        {
            return new UserConnection
            {
                UserId = request.UserId,
                Callback = callback,
                Context = request.Context,
                MatchCode = request.MatchCode
            };
        }

        private bool TryAddUser(string username, UserConnection userConnection, IChatManagerCallback callback)
        {
            if (!ConnectedUsers.TryAdd(username, userConnection))
            {
                SafeCallbackInvoke(username, () =>
                {
                    callback.ReceiveSystemNotification(
                        ChatResultCode.Chat_UserAlreadyConnected,
                        "User already connected"
                    );
                });
                return false;
            }
            return true;
        }

        private void NotifySuccessfulConnection(ChatConnectionRequest request)
        {
            var existingUsersInMatch = GetUsersInMatch(request.MatchCode);

            SafeCallbackInvoke(request.Username, () =>
            {
                ConnectedUsers[request.Username].Callback.UpdateUserList(existingUsersInMatch);
            });

            BroadcastSystemNotificationToMatch(
                ChatResultCode.Chat_UserConnected,
                $"{request.Username} has joined",
                request.MatchCode
            );

            UpdateUserListForMatch(request.MatchCode);

            loggerHelper.LogInfo(
                $"[CHAT] User connected: {request.Username} (UserId={request.UserId}, Match={request.MatchCode})");
        }

        private List<string> GetUsersInMatch(string matchCode)
        {
            return ConnectedUsers
                .Where(u => u.Value.MatchCode == matchCode)
                .Select(u => u.Key)
                .ToList();
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
                if (result.IsGuest)
                {
                    NotifyGuestMessageBlocked(username);
                }
                else if (result.ShouldBan)
                {
                    var banContext = new UserBanContext
                    {
                        Username = username,
                        Strikes = result.CurrentStrikes,
                        Context = user.Context,
                        MatchCode = user.MatchCode,
                        IsGuest = result.IsGuest,
                        UserId = user.UserId
                    };
                    HandleUserBan(banContext);
                }
                else
                {
                    NotifyUserMessageBlocked(username, result.CurrentStrikes);
                }

                return;
            }

            BroadcastMessageToMatch(username, message, user.MatchCode);
        }

        private void HandleUserBan(UserBanContext banContext)
        {
            if (banContext.IsGuest)
            {
                loggerHelper.LogWarning($"Guest {banContext.Username} expelled for inappropriate behavior");

                if (ConnectedUsers.TryGetValue(banContext.Username, out var user))
                {
                    SafeCallbackInvoke(banContext.Username, () =>
                    {
                        user.Callback.ReceiveSystemNotification(
                            ChatResultCode.Chat_UserBanned,
                            "You have been expelled for inappropriate behavior"
                        );
                    });
                }

                BroadcastSystemNotificationToMatch(
                    ChatResultCode.Chat_UserBanned,
                    $"{banContext.Username} has been expelled for inappropriate behavior",
                    banContext.MatchCode
                );

                if (banContext.Context == ContextLobby)
                    lobbyNotifier?.NotifyPlayerExpelled(banContext.MatchCode, banContext.UserId, "Inappropriate language");
                else
                    gameNotifier?.NotifyPlayerExpelled(banContext.MatchCode, banContext.UserId, "Inappropriate language");

                ConnectedUsers.TryRemove(banContext.Username, out _);
                UpdateUserListForMatch(banContext.MatchCode);

                if (banContext.Context == ContextLobby)
                    CheckMinimumPlayersInLobby(banContext.MatchCode);
                else
                    CheckMinimumPlayersInGame(banContext.MatchCode);

                return;
            }

            loggerHelper.LogWarning($"User {banContext.Username} banned with {banContext.Strikes} strikes");

            if (ConnectedUsers.TryGetValue(banContext.Username, out var registeredUser))
            {
                SafeCallbackInvoke(banContext.Username, () =>
                {
                    registeredUser.Callback.UserBannedFromChat(banContext.Username, banContext.Strikes);
                });
            }

            BroadcastSystemNotificationToMatch(
                ChatResultCode.Chat_UserBanned,
                $"{banContext.Username} has been expelled for inappropriate behavior",
                banContext.MatchCode
            );

            if (banContext.Context == ContextLobby)
                lobbyNotifier?.NotifyPlayerExpelled(banContext.MatchCode, registeredUser.UserId, "Inappropriate language");
            else
                gameNotifier?.NotifyPlayerExpelled(banContext.MatchCode, registeredUser.UserId, "Inappropriate language");

            ConnectedUsers.TryRemove(banContext.Username, out _);
            UpdateUserListForMatch(banContext.MatchCode);

            if (banContext.Context == ContextLobby)
                CheckMinimumPlayersInLobby(banContext.MatchCode);
            else
                CheckMinimumPlayersInGame(banContext.MatchCode);
        }

        private void NotifyGuestMessageBlocked(string username)
        {
            if (ConnectedUsers.TryGetValue(username, out var userConnection))
            {
                SafeCallbackInvoke(username, () =>
                {
                    userConnection.Callback.ReceiveSystemNotification(
                        ChatResultCode.Chat_MessageBlocked,
                        "Your message contains inappropriate content and cannot be sent."
                    );
                });
            }
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
