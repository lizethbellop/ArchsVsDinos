using ArchsVsDinosServer.BusinessLogic.ProfileManagement;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Services;
using ArchsVsDinosServer.Utils;
using Contracts;
using Contracts.DTO;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.ServiceModel;

namespace ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement
{
    public class LobbyConfiguration
    {
        private readonly ConcurrentDictionary<string, Lobby> ActiveMatches = new ConcurrentDictionary<string, Lobby>();
        private static readonly ConcurrentDictionary<int, List<LobbyPlayerDTO>> gameMatchPlayers = new ConcurrentDictionary<int, List<LobbyPlayerDTO>>();
        private readonly ILoggerHelper loggerHelper;

        public LobbyConfiguration()
        {
            loggerHelper = new Wrappers.LoggerHelperWrapper();
        }

        private bool TryGetCallback(out ILobbyManagerCallback lobbyCallback)
        {
            try
            {
                lobbyCallback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();
                return true;
            }
            catch
            {
                lobbyCallback = null;
                return false;
            }
        }

        private LobbyPlayerDTO CreatePlayer(UserAccountDTO userAccount, bool isHost)
        {
            var playerProfile = new ProfileInformation().GetPlayerByUsername(userAccount.Username);
            return PlayerCreator.FromLogin(userAccount, playerProfile, isHost);
        }

        private List<ILobbyManagerCallback> BroadcastToCallbacks(IEnumerable<ILobbyManagerCallback> callbackList, Action<ILobbyManagerCallback> notifyAction)
        {
            var failedCallbacks = new List<ILobbyManagerCallback>();

            foreach (var callback in callbackList)
            {
                try
                {
                    notifyAction(callback);
                }
                catch
                {
                    failedCallbacks.Add(callback);
                }
            }

            return failedCallbacks;
        }

        private Lobby FindLobbyByUsername(string username)
        {
            foreach (var lobby in ActiveMatches.Values)
            {
                if (lobby.Players.Any(player => player.Username == username))
                {
                    return lobby;
                }
            }
            return null;
        }

        private bool IsUserHost(Lobby lobby, string username)
        {
            var hostPlayer = lobby.Players.FirstOrDefault(player => player.IsHost);
            return hostPlayer != null && hostPlayer.Username == username;
        }

        private string GenerateUniqueMatchCode()
        {
            string code;
            do
            {
                code = CodeGenerator.GenerateMatchCode();
            } while (ActiveMatches.ContainsKey(code));
            return code;
        }

        public LobbyResultCode CreateANewMatch(UserAccountDTO hostUser)
        {
            ILobbyManagerCallback hostCallback;

            if (!TryGetCallback(out hostCallback))
            {
                return LobbyResultCode.Lobby_ConnectionError;
            }
                
            try
            {
                string matchCode = GenerateUniqueMatchCode();
                LobbyPlayerDTO hostPlayer = CreatePlayer(hostUser, true);

                Lobby newLobby = new Lobby();
                newLobby.MatchCode = matchCode;
                newLobby.AddPlayer(hostPlayer);
                newLobby.AddCallback(hostCallback, hostUser.Username);

                if (!ActiveMatches.TryAdd(matchCode, newLobby))
                {
                    return LobbyResultCode.Lobby_LobbyCreationError;
                }
                    
                hostCallback.CreatedLobby(hostPlayer, matchCode);

                return LobbyResultCode.Lobby_LobbyCreated;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Unexpected error creating lobby.", ex);
                return LobbyResultCode.Lobby_LobbyCreationError;
            }
        }

        public LobbyResultCode JoinToTheLobbyWithCode(UserAccountDTO joiningUser, string matchCode)
        {
            Lobby targetLobby;

            if (!ActiveMatches.TryGetValue(matchCode, out targetLobby))
            {
                return LobbyResultCode.Lobby_NotExist;
            }
                
            ILobbyManagerCallback joiningCallback;

            if (!TryGetCallback(out joiningCallback))
            {
                return LobbyResultCode.Lobby_ConnectionError;
            }
                
            lock (targetLobby)
            {
                if (targetLobby.Players.Count >= 4)
                {
                    return LobbyResultCode.Lobby_FullLobby;
                }
                    
                LobbyPlayerDTO newJoiningPlayer = CreatePlayer(joiningUser, false);
                targetLobby.AddCallback(joiningCallback, joiningUser.Username);

                var hostPlayer = targetLobby.Players.FirstOrDefault(player => player.IsHost);
                if (hostPlayer != null)
                {
                    joiningCallback.CreatedLobby(hostPlayer, targetLobby.MatchCode);
                }

                foreach (var existingPlayer in targetLobby.Players)
                {
                    joiningCallback.JoinedLobby(existingPlayer);
                }

                targetLobby.AddPlayer(newJoiningPlayer);

                var failedOnBroadcast = BroadcastToCallbacks(targetLobby.Callbacks,
                    callback => callback.JoinedLobby(newJoiningPlayer));

                foreach (var failed in failedOnBroadcast)
                {
                    targetLobby.Callbacks.Remove(failed);
                }

                return LobbyResultCode.Lobby_LobbyJoined;
            }
        }

        public LobbyResultCode CancelTheLobby(string matchCode, string requesterUsername)
        {
            Lobby targetLobby;

            if (!ActiveMatches.TryGetValue(matchCode, out targetLobby))
            {
                return LobbyResultCode.Lobby_NotExist;
            }
                
            if (!IsUserHost(targetLobby, requesterUsername))
            {
                return LobbyResultCode.Lobby_NotHost;
            }
                
            lock (targetLobby)
            {
                List<ILobbyManagerCallback> failedCallbacks =
                    BroadcastToCallbacks(targetLobby.Callbacks, callback =>
                        callback.LobbyCancelled(matchCode));

                foreach (var failed in failedCallbacks)
                {
                    targetLobby.Callbacks.Remove(failed);
                }

                ActiveMatches.TryRemove(matchCode, out targetLobby);

                return LobbyResultCode.Lobby_LobbyCancelled;
            }
        }

        public LobbyResultCode LeaveTheLobby(string username)
        {
            Lobby targetLobby = FindLobbyByUsername(username);

            if (targetLobby == null)
            {
                return LobbyResultCode.Lobby_NotExist;
            }
                
            LobbyPlayerDTO leavingPlayer =
                targetLobby.Players.FirstOrDefault(player => player.Username == username);

            if (leavingPlayer == null)
            {
                return LobbyResultCode.Lobby_LobbyLeftError;
            }
                
            if (leavingPlayer.IsHost)
            {
                return CancelTheLobby(targetLobby.MatchCode, username);
            }      

            lock (targetLobby)
            {
                targetLobby.Players.Remove(leavingPlayer);

                List<ILobbyManagerCallback> failedCallbacks =
                    BroadcastToCallbacks(targetLobby.Callbacks, callback =>
                        callback.LeftLobby(leavingPlayer));

                foreach (var failed in failedCallbacks)
                {
                    targetLobby.Callbacks.Remove(failed);
                }

                return LobbyResultCode.Lobby_LobbyLeft;
            }
        }

        public LobbyResultCode ExpelThePlayer(string usernameToExpel, string hostUsername)
        {
            Lobby targetLobby = FindLobbyByUsername(usernameToExpel);

            if (targetLobby == null)
                return LobbyResultCode.Lobby_NotExist;

            if (!IsUserHost(targetLobby, hostUsername))
                return LobbyResultCode.Lobby_NotHost;

            lock (targetLobby)
            {
                LobbyPlayerDTO playerToExpel = targetLobby.Players
                    .FirstOrDefault(player => player.Username == usernameToExpel);

                if (playerToExpel == null)
                    return LobbyResultCode.Lobby_PlayerExpelledError;

                targetLobby.Players.Remove(playerToExpel);

                var failedLeftCallbacks = BroadcastToCallbacks(
                    targetLobby.Callbacks,
                    callback => callback.LeftLobby(playerToExpel)
                );

                foreach (var failed in failedLeftCallbacks)
                    targetLobby.Callbacks.Remove(failed);

                NotifyPlayerExpelled(targetLobby, playerToExpel);

                return LobbyResultCode.Lobby_PlayerExpelled;
            }
        }

        private void NotifyPlayerExpelled(Lobby lobby, LobbyPlayerDTO expelledPlayer)
        {
            try
            {
                var callback = lobby.CallbackOwners
                    .FirstOrDefault(pair => pair.Value == expelledPlayer.Username)
                    .Key;

                if (callback != null)
                {
                    try
                    {
                        callback.ExpelledFromLobby(expelledPlayer);
                    }
                    catch
                    {
                        lobby.Callbacks.Remove(callback);
                        lobby.CallbackOwners.Remove(callback);
                    }
                }
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error notifying expelled player. ", ex);
            }
        }

        public LobbyResultCode StartTheGame(string matchCode, string hostUsername)
        {
            Lobby targetLobby;

            if (!ActiveMatches.TryGetValue(matchCode, out targetLobby))
            {
                return LobbyResultCode.Lobby_NotExist;
            }

            if (!IsUserHost(targetLobby, hostUsername))
            {
                return LobbyResultCode.Lobby_NotHost;
            }

            lock (targetLobby)
            {
                if (targetLobby.Players.Count < 2)
                {
                    return LobbyResultCode.Lobby_IncompleteLobby;
                }

                string gameMatchCode = GenerateAndRegisterGameCode(matchCode);

                if (gameMatchCode == null)
                {
                    return LobbyResultCode.Lobby_LobbyCreationError;
                }

                NotifyPlayersGameStarted(targetLobby, gameMatchCode);
                RemoveLobbyFromActive(matchCode);

                return LobbyResultCode.Lobby_GameStarted;
            }
        }

        private string GenerateAndRegisterGameCode(string lobbyCode)
        {
            string gameMatchCode = CodeGenerator.GenerateGameMatchCode(lobbyCode);
            int matchId = Math.Abs(gameMatchCode.GetHashCode());
            var sessionManager = GameManagement.GameSessionManager.Instance;

            if (!sessionManager.RegisterGameCode(gameMatchCode, matchId))
            {
                loggerHelper.LogInfo($"Failed to register game code: {gameMatchCode}");
                return null;
            }

            Lobby lobby;
            if (ActiveMatches.TryGetValue(lobbyCode, out lobby))
            {
                var playersCopy = new List<LobbyPlayerDTO>(lobby.Players);
                gameMatchPlayers.TryAdd(matchId, playersCopy);
                loggerHelper.LogInfo($"Saved {playersCopy.Count} players for matchId: {matchId}");
            }

            return gameMatchCode;
        }

        private void NotifyPlayersGameStarted(Lobby lobby, string gameMatchCode)
        {
            var failedCallbacks = BroadcastToCallbacks(
                lobby.Callbacks,
                callback => callback.GameStarted(gameMatchCode, lobby.Players)
            );

            foreach (var failed in failedCallbacks)
            {
                lobby.Callbacks.Remove(failed);
            }
        }

        private void RemoveLobbyFromActive(string matchCode)
        {
            ActiveMatches.TryRemove(matchCode, out _);
        }

        public LobbyResultCode InvitByAnEmailToMatch(string email, string matchCode, string inviterUsername)
        {
            try
            {
                EmailService.SendLobbyInvitation(email, matchCode, inviterUsername);
                return LobbyResultCode.Lobby_EmailSended;
            }
            catch (SmtpException ex)
            {
                loggerHelper.LogError($"Error sending email invitation", ex);
                return LobbyResultCode.Lobby_EmailSendError;
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Unexpected error sending email", ex);
                return LobbyResultCode.Lobby_EmailSendError;
            }
        }

        public List<LobbyPlayerDTO> GetPlayersForGameMatch(int matchId)
        {
            if (gameMatchPlayers.TryRemove(matchId, out var players))
            {
                loggerHelper.LogInfo($"Retrieved {players.Count} players for matchId: {matchId}");
                return players;
            }

            loggerHelper.LogWarning($"No players found for matchId: {matchId}");
            return new List<LobbyPlayerDTO>();
        }

        /*
        
        public void ExpelPlayerByStrike(string username, string reason)
        {
            try
            {
                Lobby targetLobby = FindLobbyByUsername(username);

                if (targetLobby == null)
                {
                    LoggerHelper.LogInfo($"No se encontró lobby para el usuario {username}");
                    return;
                }

                lock (targetLobby)
                {
                    LobbyPlayerDTO expelledPlayer = targetLobby.Players
                        .FirstOrDefault(player => player.Username == username);

                    if (expelledPlayer == null)
                    {
                        LoggerHelper.LogInfo($"Jugador {username} no encontrado en el lobby");
                        return;
                    }

                    // Remover del lobby
                    targetLobby.Players.Remove(expelledPlayer);

                    // Notificar a todos
                    List<ILobbyManagerCallback> failedCallbacks = BroadcastToCallbacks(
                        targetLobby.Callbacks,
                        callback => callback.PlayerExpelledBySystem(expelledPlayer, reason)
                    );

                    foreach (var failed in failedCallbacks)
                    {
                        targetLobby.Callbacks.Remove(failed);
                    }

                    LoggerHelper.LogInfo($"Jugador {username} expulsado del lobby {targetLobby.MatchCode}: {reason}");

                }
            }
            catch (InvalidOperationException ex)
            {
                LoggerHelper.LogError($"Operación inválida al expulsar a {username}", ex);
            }
            catch (CommunicationException ex)
            {
                LoggerHelper.LogError($"Error de comunicación al expulsar a {username}", ex);
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Error inesperado al expulsar a {username}", ex);
            }
        }*/

    }
}
