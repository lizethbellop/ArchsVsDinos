using ArchsVsDinosServer.BusinessLogic.ProfileManagement;
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
                Console.WriteLine($"Error obtaining the callback: {ex.Message}");
                return LobbyResultCode.Lobby_ConnectionError;
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine($"Timeout while notifying: {ex.Message}");
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
                Console.WriteLine($"Timeout creating the match: {ex.Message}");
                return LobbyResultCode.Lobby_LobbyCreationError;
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
            catch (TimeoutException)
            {
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
            catch (TimeoutException)
            {
                return LobbyResultCode.Lobby_LobbyJoinedError;
            }
            catch
            {
                return LobbyResultCode.Lobby_LobbyJoinedError;
            }

        }

        public LobbyResultCode CancelTheLobby(string matchCode, string usernameRequester)
        {

            if (!ActiveMatches.TryGetValue(matchCode, out Lobby lobbyToCancel))
            {
                return LobbyResultCode.Lobby_NotExist;
            }

            var hostPlayer = lobbyToCancel.Players.FirstOrDefault(player => player.IsHost);

            if (hostPlayer == null || hostPlayer.Username != usernameRequester)
            {
                return LobbyResultCode.Lobby_NotHost;
            }

            foreach (var callback in lobbyToCancel.Callbacks.ToList())
            {
                try
                {
                    callback.LobbyCancelled(matchCode);
                }
                catch (CommunicationException ex)
                {
                    Console.WriteLine($"Error obtaining the callback: {ex.Message}");
                    return LobbyResultCode.Lobby_LobbyCancelationError;
                }
                catch (TimeoutException ex)
                {
                    Console.WriteLine($"Timeout cancelling the lobby: {ex.Message}");
                    return LobbyResultCode.Lobby_LobbyCancelationError;
                }
            }

            Lobby removedLobbyReference;
            ActiveMatches.TryRemove(matchCode, out removedLobbyReference);

            return LobbyResultCode.Lobby_LobbyCancelled;
        }
    }
}
