using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Lobby;
using ArchsVsDinosServer.Model;
using ArchsVsDinosServer.Wrappers;
using Contracts;
using log4net.Core;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement
{
    public class LobbySession : ILobbySession
    {
        private readonly ILoggerHelper logger;
        private static readonly Lazy<LobbySession> instance = new Lazy<LobbySession>(() => new LobbySession(new LoggerHelperWrapper()));

        public static LobbySession Instance => instance.Value;

        private readonly Dictionary<string, ActiveLobbyData> activeLobbies;
        private readonly Dictionary<string, Dictionary<string, ILobbyManagerCallback>> lobbyCallbacks;

        private readonly object syncRoot = new object();

        private LobbySession(ILoggerHelper logger)
        {
            this.logger = logger;
            activeLobbies = new Dictionary<string, ActiveLobbyData>();
            lobbyCallbacks = new Dictionary<string, Dictionary<string, ILobbyManagerCallback>>();
        }
        public void Broadcast(string lobbyCode, Action<ILobbyManagerCallback> broadcastAction)
        {
            Dictionary<string, ILobbyManagerCallback> callbacksCopy;

            lock (syncRoot)
            {
                if (!lobbyCallbacks.ContainsKey(lobbyCode))
                {
                    return;
                }
                callbacksCopy = new Dictionary<string, ILobbyManagerCallback>(lobbyCallbacks[lobbyCode]);
            }

            var disconnectedPlayers = new List<string>();

            foreach (var entry in callbacksCopy)
            {
                var playerName = entry.Key;
                var callback = entry.Value;

                try
                {
                    if (callback is ICommunicationObject comm &&
                        comm.State == CommunicationState.Opened)
                    {
                        broadcastAction(callback);
                    }
                    else
                    {
                        disconnectedPlayers.Add(playerName);
                    }
                }
                catch (ObjectDisposedException)
                {
                    disconnectedPlayers.Add(playerName);
                }
                catch (TimeoutException)
                {
                    disconnectedPlayers.Add(playerName);
                    logger.LogWarning($"Timeout broadcasting to {playerName} in {lobbyCode}");
                }
                catch (CommunicationException ex)
                {
                    disconnectedPlayers.Add(playerName);
                    logger.LogWarning($"Communication error with {playerName}: {ex.Message}");
                }
                catch (InvalidOperationException ex)
                {
                    disconnectedPlayers.Add(playerName);
                    logger.LogError("Invalid operation during broadcast", ex);
                }
            }

            if (disconnectedPlayers.Any())
            {
                CleanupDisconnectedPlayers(lobbyCode, disconnectedPlayers);
            }
        }

        public void CreateLobby(string lobbyCode, ActiveLobbyData lobbyData)
        {
            lock(syncRoot)
            {
                if (activeLobbies.ContainsKey(lobbyCode))
                {
                    return;
                }

                activeLobbies.Add(lobbyCode, lobbyData);
                lobbyCallbacks.Add(lobbyCode, new Dictionary<string, ILobbyManagerCallback>());
            }
        }

        public ActiveLobbyData GetLobby(string lobbyCode)
        {
            lock (syncRoot)
            {
                return activeLobbies.TryGetValue(lobbyCode, out var lobby) ? lobby : null;
            }
        }

        public bool LobbyExists(string lobbyCode)
        {
            lock (syncRoot)
            {
                return activeLobbies.ContainsKey(lobbyCode);
            }
        }

        public void ConnectPlayerCallback(string lobbyCode, string playerName, ILobbyManagerCallback callback)
        {
            lock (syncRoot)
            {
                if (lobbyCallbacks.ContainsKey(lobbyCode))
                {
                    lobbyCallbacks[lobbyCode][playerName] = callback;
                }
            }
        }

        public void RemoveLobby(string lobbyCode)
        {
            lock (syncRoot)
            {
                activeLobbies.Remove(lobbyCode);
                lobbyCallbacks.Remove(lobbyCode);
            }
        }

        public void DisconnectPlayerCallback(string lobbyCode, string playerName)
        {
            lock (syncRoot)
            {
                if(lobbyCallbacks.ContainsKey(lobbyCode))
                {
                    lobbyCallbacks[lobbyCode].Remove(playerName);
                }
            }
        }

        private void CleanupDisconnectedPlayers(
            string lobbyCode,
            List<string> players)
        {
            lock (syncRoot)
            {
                if (!lobbyCallbacks.ContainsKey(lobbyCode))
                {
                    return;
                }

                foreach (var player in players)
                {
                    lobbyCallbacks[lobbyCode].Remove(player);
                    logger.LogInfo($"Removed inactive player {player} from {lobbyCode}");
                }
            }
        }

        public IEnumerable<ActiveLobbyData> GetAllLobbies()
        {
            lock (syncRoot)
            {
                return activeLobbies.Values.ToList();
            }
        }

        public ILobbyManagerCallback GetPlayerCallbackByUsername(string lobbyCode, string username)
        {
            lock (syncRoot)
            {
                if (!lobbyCallbacks.ContainsKey(lobbyCode))
                {
                    return null;
                }

                var lobby = GetLobby(lobbyCode);
                if (lobby == null)
                {
                    return null;
                }

                var player = lobby.Players.FirstOrDefault(p =>
                    p.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (player == null)
                {
                    return null;
                }

                return lobbyCallbacks[lobbyCode].TryGetValue(player.Nickname, out var callback)
                    ? callback
                    : null;
            }
        }

        public ILobbyManagerCallback FindUserCallbackInAnyLobby(string username)
        {
            lock (syncRoot)
            {
                foreach (var lobbyCode in activeLobbies.Keys)
                {
                    var callback = GetPlayerCallbackByUsername(lobbyCode, username);
                    if (callback != null)
                    {
                        return callback;
                    }
                }
                return null;
            }
        }

        public ActiveLobbyData FindLobbyByPlayerNickname(string playerNickname)
        {
            if (string.IsNullOrWhiteSpace(playerNickname))
            {
                return null;
            }

            lock (syncRoot)
            {
                foreach (var kvp in activeLobbies)
                {
                    var lobby = kvp.Value;
                    lock (lobby.LobbyLock)
                    {
                        var player = lobby.Players.FirstOrDefault(p =>
                            p.Nickname.Equals(playerNickname, StringComparison.OrdinalIgnoreCase));

                        if (player != null)
                        {
                            return lobby;
                        }
                    }
                }
            }

            return null;
        }

    }
}
