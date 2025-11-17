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

                PlayerDTO player = new ProfileInformation().GetPlayerByUsername(hostUserAccountDTO.Username);

                var hostPlayer = PlayerCreator.FromLogin(
                    hostUserAccountDTO,
                    player,
                    true
                );

                var matchLobby = new Lobby
                {
                    MatchCode = matchCode,
                    LobbyCallback = callback,
                };
                matchLobby.AddPlayer(hostPlayer);

                if (!ActiveMatches.TryAdd(matchCode, matchLobby)){
                    return LobbyResultCode.Lobby_LobbyCreationError;
                }

                return LobbyResultCode.Lobby_LobbyCreated;
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine($"Timeout creating the match: {ex.Message}");
                return LobbyResultCode.Lobby_LobbyCreationError;
            }
        }
    }

}
