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
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement
{
    public class LobbyConfiguration
    {

        private readonly ConcurrentDictionary<string, Lobby> ActiveMatches = new ConcurrentDictionary<string, Lobby>();

        public LobbyResultCode CreateANewMatch(UserAccountDTO hostUserAccountDTO)
        {
            ILobbyManagerCallback callback;

            try
            {
                callback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();
            }
            catch (CommunicationException ex)
            {
                LoggerHelper.LogError($"Error obtaining the callback.", ex);
                return LobbyResultCode.Lobby_ConnectionError;
            }
            catch (TimeoutException ex)
            {
                LoggerHelper.LogError($"Timeout while notifying.", ex);
                return LobbyResultCode.Lobby_ConnectionError;
            }

            try
            {
                string matchCode = CodeGenerator.GenerateMatchCode();

                var profile = new ProfileInformation().GetPlayerByUsername(hostUserAccountDTO.Username);

                var hostPlayer = PlayerCreator.FromLogin(
                    hostUserAccountDTO,
                    profile,
                    true
                );

                var matchLobby = new Lobby
                {
                    MatchCode = matchCode,
                };

                matchLobby.AddPlayer(hostPlayer);
                matchLobby.AddCallback(callback);

                if (!ActiveMatches.TryAdd(matchCode, matchLobby))
                {
                    return LobbyResultCode.Lobby_LobbyCreationError;
                }

                callback.CreatedLobby(hostPlayer, matchCode);

                return LobbyResultCode.Lobby_LobbyCreated;
            }
            catch (TimeoutException ex)
            {
                LoggerHelper.LogError($"Timeout creating the lobby.", ex);
                return LobbyResultCode.Lobby_LobbyCreationError;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Unexpected error creating the lobby.", ex);
                return LobbyResultCode.Lobby_LobbyJoinedError;
            }
        }

        public LobbyResultCode JoinToTheLobbyWithCode(UserAccountDTO userAccountDTO, string matchCode)
        {
            if (!ActiveMatches.TryGetValue(matchCode, out Lobby matchLobby))
            {
                return LobbyResultCode.Lobby_NotExist;
            }

            ILobbyManagerCallback callback;

            try
            {
                callback = OperationContext.Current.GetCallbackChannel<ILobbyManagerCallback>();
            }
            catch (CommunicationException ex)
            {
                LoggerHelper.LogError($"Error obtaining the callback.", ex);
                return LobbyResultCode.Lobby_ConnectionError;
            }
            catch (TimeoutException ex)
            {
                LoggerHelper.LogError($"Timeout while notifying.", ex);
                return LobbyResultCode.Lobby_ConnectionError;
            }

            try
            {
                var profile = new ProfileInformation().GetPlayerByUsername(userAccountDTO.Username);

                var newPlayer = PlayerCreator.FromLogin(
                    userAccountDTO,
                    profile,
                    false
                );

                matchLobby.AddPlayer(newPlayer);

                matchLobby.AddCallback(callback);

                foreach (var playerCallback in matchLobby.Callbacks)
                {
                    try
                    {
                        playerCallback.JoinedLobby(newPlayer);
                    }
                    catch (CommunicationException)
                    {
                        matchLobby.Callbacks.Remove(playerCallback);
                    }
                    catch (TimeoutException)
                    {
                        matchLobby.Callbacks.Remove(playerCallback);
                    }
                }

                return LobbyResultCode.Lobby_LobbyJoined;
            }
            catch (TimeoutException ex)
            {
                LoggerHelper.LogError($"Timeout joining to the lobby.", ex);
                return LobbyResultCode.Lobby_LobbyJoinedError;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Unexpected error joining to the lobby.", ex);
                return LobbyResultCode.Lobby_LobbyJoinedError;
            }

        }

        public LobbyResultCode CancelTheLobby(string matchCode, string usernameRequester)
        {
            try
            {
                if (!ActiveMatches.TryGetValue(matchCode, out Lobby lobbyToCancel))
                {
                    return LobbyResultCode.Lobby_NotExist;
                }

                LobbyPlayerDTO hostPlayer = lobbyToCancel.Players.FirstOrDefault(player => player.IsHost);
                if (hostPlayer == null || hostPlayer.Username != usernameRequester)
                {
                    return LobbyResultCode.Lobby_NotHost;
                }

                List<ILobbyManagerCallback> disconnectedCallbacks = new List<ILobbyManagerCallback>();

                foreach (ILobbyManagerCallback callback in lobbyToCancel.Callbacks)
                {
                    try
                    {
                        callback.LobbyCancelled(matchCode);
                    }
                    catch (CommunicationException)
                    {
                        disconnectedCallbacks.Add(callback);
                    }
                    catch (TimeoutException)
                    {
                        disconnectedCallbacks.Add(callback);
                    }
                    catch (Exception)
                    {
                        disconnectedCallbacks.Add(callback);
                    }
                }

                foreach (ILobbyManagerCallback invalidCallback in disconnectedCallbacks)
                {
                    lobbyToCancel.Callbacks.Remove(invalidCallback);
                }

                Lobby removedLobby;
                ActiveMatches.TryRemove(matchCode, out removedLobby);

                return LobbyResultCode.Lobby_LobbyCancelled;
            }
            catch (TimeoutException ex)
            {
                LoggerHelper.LogError($"Timeout cancelling the lobby.", ex);
                return LobbyResultCode.Lobby_LobbyJoinedError;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Unexpected error while cancelling the lobby.", ex);
                return LobbyResultCode.Lobby_LobbyCancelationError;
            }
        }

        public LobbyResultCode LeaveTheLobby(string username)
        {
            try
            {
                Lobby lobbyToLeave = ActiveMatches.Values.FirstOrDefault(l => l.Players.Any(p => p.Username == username));
                if (lobbyToLeave == null)
                {
                    return LobbyResultCode.Lobby_NotExist;
                }

                LobbyPlayerDTO leavingPlayer = lobbyToLeave.Players.FirstOrDefault(player => player.Username == username);
                if (leavingPlayer == null)
                {
                    return LobbyResultCode.Lobby_LobbyLeftError;
                }

                if (leavingPlayer.IsHost)
                {
                    return CancelTheLobby(lobbyToLeave.MatchCode, username);
                }

                lobbyToLeave.Players.Remove(leavingPlayer);

                List<ILobbyManagerCallback> disconnectedCallbacks = new List<ILobbyManagerCallback>();

                foreach (ILobbyManagerCallback callback in lobbyToLeave.Callbacks)
                {
                    try
                    {
                        callback.LeftLobby(leavingPlayer);
                    }
                    catch (CommunicationException)
                    {
                        disconnectedCallbacks.Add(callback);
                    }
                    catch (TimeoutException)
                    {
                        disconnectedCallbacks.Add(callback);
                    }
                    catch (Exception)
                    {
                        disconnectedCallbacks.Add(callback);
                    }
                }

                foreach (ILobbyManagerCallback invalidCallback in disconnectedCallbacks)
                {
                    lobbyToLeave.Callbacks.Remove(invalidCallback);
                }

                return LobbyResultCode.Lobby_LobbyLeft;
            }
            catch (TimeoutException ex)
            {
                LoggerHelper.LogError($"Timeout leavibg the lobby.", ex);
                return LobbyResultCode.Lobby_LobbyJoinedError;
            }
            catch (Exception ex)
            {;
                LoggerHelper.LogError($"Unexpected error while leaving the lobby.", ex);
                return LobbyResultCode.Lobby_LobbyCancelationError;
            }
        }

        public LobbyResultCode ExpelThePlayer(string usernameToExpel, string hostUsername)
        {
            try
            {
                Lobby lobby = ActiveMatches.Values.FirstOrDefault(
                    actualLobby => actualLobby.Players.Any(player => player.Username == usernameToExpel)
                );

                if (lobby == null)
                {
                    return LobbyResultCode.Lobby_NotExist;
                }

                LobbyPlayerDTO host = lobby.Players.FirstOrDefault(p => p.IsHost);
                if (host == null || host.Username != hostUsername)
                {
                    return LobbyResultCode.Lobby_NotHost;
                }

                LobbyPlayerDTO playerToExpel = lobby.Players.FirstOrDefault(p => p.Username == usernameToExpel);
                if (playerToExpel == null)
                {
                    return LobbyResultCode.Lobby_PlayerExpelledError;
                }

                lobby.Players.Remove(playerToExpel);

                List<ILobbyManagerCallback> disconnectedCallbacks = new List<ILobbyManagerCallback>();

                foreach (var callback in lobby.Callbacks)
                {
                    try
                    {
                        callback.ExpelledFromLobby(playerToExpel);
                    }
                    catch (CommunicationException)
                    {
                        disconnectedCallbacks.Add(callback);
                    }
                    catch (TimeoutException)
                    {
                        disconnectedCallbacks.Add(callback);
                    }
                    catch (Exception)
                    {
                        disconnectedCallbacks.Add(callback);
                    }
                }

                foreach (var invalid in disconnectedCallbacks)
                {
                    lobby.Callbacks.Remove(invalid);
                }

                return LobbyResultCode.Lobby_PlayerExpelled;
            }
            catch (TimeoutException ex)
            {
                LoggerHelper.LogError("Timeout expelling the player from lobby.", ex);
                return LobbyResultCode.Lobby_PlayerExpelledError;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError("Unexpected error expelling player from lobby.", ex);
                return LobbyResultCode.Lobby_PlayerExpelledError;
            }
        }

    }
}
