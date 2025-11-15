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
        
        private readonly ConcurrentDictionary<string, MatchLobby> ActiveMatches = new ConcurrentDictionary<string, MatchLobby>();
        //private readonly MatchLobbyCallbackManager callbackManager = new MatchLobbyCallbackManager(new LoggerHelper());

        public MatchLobbyResultCode CreateANewMatch(UserAccountDTO hostUserAccountDTO)
        {
            IMatchLobbyManagerCallback callback;

            try
            {
                callback = OperationContext.Current.GetCallbackChannel<IMatchLobbyManagerCallback>();
            }
            catch (CommunicationException ex)
            {
                Console.WriteLine($"Error obtaining the callback: {ex.Message}");
                return MatchLobbyResultCode.Lobby_ConnectionError;
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine($"Timeout while notifying: {ex.Message}");
                return MatchLobbyResultCode.Lobby_ConnectionError;
            }

            try
            {
                string matchCode = CodeGenerator.GenerateMatchCode();
                var hostPlayer = PlayerCreator.CreateHostPlayer(hostUserAccountDTO.Username, hostUserAccountDTO.Nickname);

                var matchLobby = new MatchLobby
                {
                    MatchCode = matchCode,
                    MatchLobbyCallback = callback,
                };
                matchLobby.AddPlayer(hostPlayer);

                if (!ActiveMatches.TryAdd(matchCode, matchLobby)){
                    return MatchLobbyResultCode.Lobby_MatchLobbyCreationError;
                }

                //callbackManager.CreatedMatch(hostPlayer, matchCode);

                return MatchLobbyResultCode.Lobby_MatchLobbyCreated;
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine($"Timeout creating the match: {ex.Message}");
                return MatchLobbyResultCode.Lobby_MatchLobbyCreationError;
            }
        }
    }

}
