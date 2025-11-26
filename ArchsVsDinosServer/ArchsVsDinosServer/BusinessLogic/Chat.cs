using ArchsVsDinosServer.Interfaces;
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
        public enum ChatContext
        {
            Lobby,
            InGame
        }

        private class UserConnection
        {
            public int UserId { get; set; }
            public IChatManagerCallback Callback { get; set; }
            public ChatContext Context { get; set; }
            public string MatchCode { get; set; }
        }

        private static readonly ConcurrentDictionary<string, UserConnection> ConnectedUsers = new ConcurrentDictionary<string, UserConnection>();
        private const string DefaultRoom = "Lobby";
        private const int MinimumPlayersRequired = 2;
        private readonly ILoggerHelper loggerHelper;
        private readonly StrikeManager strikeManager;
        private readonly Func<IDbContext> contextFactory;
        private readonly ILobbyNotifier lobbyNotifier;
        private readonly IGameNotifier gameNotifier;

        public Chat(
            ILoggerHelper loggerHelper,
            Func<IDbContext> contextFactory,
            ILobbyNotifier lobbyNotifier,
            IGameNotifier gameNotifier)
        {
            this.loggerHelper = loggerHelper;
            this.contextFactory = contextFactory;
            this.lobbyNotifier = lobbyNotifier;
            this.gameNotifier = gameNotifier;

            var dependencies = new ServiceDependencies(
                new Wrappers.SecurityHelperWrapper(),
                new Wrappers.ValidationHelperWrapper(),
                loggerHelper,
                contextFactory
            );

            const string DataFolder = "Data";
            const string BannedWordsFile = "bannedWords.txt";
            string bannedWordsPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                DataFolder,
                BannedWordsFile
            );

            var profanityFilter = new ProfanityFilter(loggerHelper, bannedWordsPath);
            this.strikeManager = new StrikeManager(dependencies, profanityFilter);
        }

        public void Connect(ChatConnectionRequest request)
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

            int userId = GetUserIdFromUsername(request.Username);
            if (userId == 0)
            {
                SafeCallbackInvoke(request.Username, () =>
                {
                    callback.ReceiveSystemNotification(ChatResultCode.Chat_Error, "User not found");
                });
                return;
            }

            var context = (ChatContext)request.Context;

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

            BroadcastSystemNotificationWithEnum(ChatResultCode.Chat_UserConnected, $"{request.Username} has joined");
            UpdateUserList();
        }

        public void Disconnect(string username)
        {
            if (ConnectedUsers.TryRemove(username, out var userConnection))
            {
                BroadcastSystemNotificationWithEnum(ChatResultCode.Chat_UserDisconnected, $"{username} has left");
                UpdateUserList();

                if (userConnection.Context == ChatContext.Lobby)
                {
                    CheckMinimumPlayersInLobby();
                }
                else if (userConnection.Context == ChatContext.InGame)
                {
                    CheckMinimumPlayersInGame(userConnection.MatchCode);
                }
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

                // USAR EL NUEVO ProcessStrike
                var banResult = strikeManager.ProcessStrike(userConnection.UserId, message);

                if (!banResult.CanSendMessage)
                {
                    if (banResult.ShouldBan)
                    {
                        // Usuario alcanzó 3 strikes - EXPULSAR
                        HandleUserBan(username, banResult.CurrentStrikes, userConnection.Context, userConnection.MatchCode);
                    }
                    else
                    {
                        NotifyUserMessageBlocked(username, banResult.CurrentStrikes);
                    }

                    loggerHelper.LogInfo($"Message from {username} blocked. Strikes: {banResult.CurrentStrikes}/3");
                    return;
                }

                BroadcastMessageToAll(username, message);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error sending message from {username}: {ex.Message}", ex);
            }
        }

        private void HandleUserBan(string username, int strikes, ChatContext context, string matchCode)
        {
            loggerHelper.LogWarning($"User {username} reached {strikes} strikes in {context}");

            if (ConnectedUsers.TryGetValue(username, out var userConnection))
            {
                SafeCallbackInvoke(username, () =>
                {
                    userConnection.Callback.UserBannedFromChat(username, strikes);
                });
            }

            BroadcastSystemNotificationWithEnum(
                ChatResultCode.Chat_UserBanned,
                $"{username} has been expelled for inappropriate behavior"
            );

            if (context == ChatContext.Lobby)
            {
                lobbyNotifier?.NotifyPlayerExpelled(username, "Inappropriate language");
            }
            else if (context == ChatContext.InGame)
            {
                gameNotifier?.NotifyPlayerExpelled(matchCode, username, "Inappropriate language");
            }

            ConnectedUsers.TryRemove(username, out _);
            UpdateUserList();

            if (context == ChatContext.Lobby)
            {
                CheckMinimumPlayersInLobby();
            }
            else if (context == ChatContext.InGame)
            {
                CheckMinimumPlayersInGame(matchCode);
            }
        }

        private void CheckMinimumPlayersInLobby()
        {
            int lobbyPlayers = ConnectedUsers.Count(u => u.Value.Context == ChatContext.Lobby);

            if (lobbyPlayers < MinimumPlayersRequired)
            {
                loggerHelper.LogWarning($"Insufficient players in lobby. Current: {lobbyPlayers}");

                NotifyLobbyClosing("Insufficient players. Minimum required: 2 players");

                lobbyNotifier?.NotifyLobbyClosure("Insufficient players");

                DisconnectLobbyUsers();
            }
        }

        private void CheckMinimumPlayersInGame(string matchCode)
        {
            int gamePlayers = ConnectedUsers.Count(u =>
                u.Value.Context == ChatContext.InGame &&
                u.Value.MatchCode == matchCode);

            if (gamePlayers < MinimumPlayersRequired)
            {
                loggerHelper.LogWarning($"Insufficient players in game {matchCode}. Current: {gamePlayers}");

                NotifyGameClosing(matchCode, "Insufficient players to continue");

                gameNotifier?.NotifyGameClosure(matchCode, "Insufficient players");

                DisconnectGameUsers(matchCode);
            }
        }

        private void NotifyGameClosing(string matchCode, string reason)
        {
            foreach (var user in ConnectedUsers.Where(u => u.Value.MatchCode == matchCode).ToArray())
            {
                SafeCallbackInvoke(user.Key, () =>
                {
                    user.Value.Callback.LobbyClosedDueToInsufficientPlayers(reason);
                });
            }
        }

        private void NotifyLobbyClosing(string reason)
        {
            foreach (var user in ConnectedUsers.Where(u => u.Value.Context == ChatContext.Lobby).ToArray())
            {
                SafeCallbackInvoke(user.Key, () =>
                {
                    user.Value.Callback.LobbyClosedDueToInsufficientPlayers(reason);
                });
            }
        }

        private void DisconnectLobbyUsers()
        {
            var lobbyUsers = ConnectedUsers.Where(u => u.Value.Context == ChatContext.Lobby).Select(u => u.Key).ToList();

            foreach (var username in lobbyUsers)
            {
                ConnectedUsers.TryRemove(username, out _);
            }
        }

        private void DisconnectGameUsers(string matchCode)
        {
            var gameUsers = ConnectedUsers
                .Where(u => u.Value.Context == ChatContext.InGame && u.Value.MatchCode == matchCode)
                .Select(u => u.Key)
                .ToList();

            foreach (var username in gameUsers)
            {
                ConnectedUsers.TryRemove(username, out _);
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
