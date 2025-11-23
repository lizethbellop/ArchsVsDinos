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
using System.ServiceModel;

namespace ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement
{
    public class LobbyConfiguration
    {
        private readonly ConcurrentDictionary<string, Lobby> ActiveMatches = new ConcurrentDictionary<string, Lobby>();

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

        public LobbyResultCode CreateANewMatch(UserAccountDTO hostUser)
        {
            ILobbyManagerCallback hostCallback;

            if (!TryGetCallback(out hostCallback))
            {
                return LobbyResultCode.Lobby_ConnectionError;
            }
                
            try
            {
                string matchCode = CodeGenerator.GenerateMatchCode();
                LobbyPlayerDTO hostPlayer = CreatePlayer(hostUser, true);

                Lobby newLobby = new Lobby();
                newLobby.MatchCode = matchCode;
                newLobby.AddPlayer(hostPlayer);
                newLobby.AddCallback(hostCallback);

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
                targetLobby.AddCallback(joiningCallback);

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
            {
                return LobbyResultCode.Lobby_NotExist;
            }
                

            if (!IsUserHost(targetLobby, hostUsername))
            {
                return LobbyResultCode.Lobby_NotHost;
            }
                
            lock (targetLobby)
            {
                LobbyPlayerDTO expelledPlayer =
                    targetLobby.Players.FirstOrDefault(player => player.Username == usernameToExpel);

                if (expelledPlayer == null)
                {
                    return LobbyResultCode.Lobby_PlayerExpelledError;
                }
                   
                targetLobby.Players.Remove(expelledPlayer);

                List<ILobbyManagerCallback> failedCallbacks =
                    BroadcastToCallbacks(targetLobby.Callbacks, callback =>
                        callback.ExpelledFromLobby(expelledPlayer));

                foreach (var failed in failedCallbacks)
                {
                    targetLobby.Callbacks.Remove(failed);
                }

                return LobbyResultCode.Lobby_PlayerExpelled;
            }
        }
    }
}
