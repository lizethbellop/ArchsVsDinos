using ArchsVsDinosServer.BusinessLogic.ProfileManagement;
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

        public MatchLobbyResultCode CreateANewMatch(UserAccountDTO hostUserAccountDTO)
        {
            IMatchLobbyManagerCallback callback = null;

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

                ProfileInformation profileInfo = new ProfileInformation();
                PlayerDTO hostPlayer = profileInfo.GetPlayerByUsername(hostUserAccountDTO.username);

                LobbyPlayerDTO lobbyHostPlayer = new LobbyPlayerDTO
                {
                    username = hostUserAccountDTO.username,
                    nickname = hostUserAccountDTO.nickname,
                    profilePicture = hostPlayer?.profilePicture,
                    isHost = true
                };

                MatchLobby matchLobby = new MatchLobby
                {
                    matchCode = matchCode,
                    players = new List<LobbyPlayerDTO> { lobbyHostPlayer },
                    matchLobbyCallback = callback
                };

                if (!ActiveMatches.TryAdd(matchCode, matchLobby))
                    return MatchLobbyResultCode.Lobby_MatchLobbyCreationError;

                try
                {
                    callback.CreatedMatch(lobbyHostPlayer, matchCode);
                }
                catch (CommunicationException ex)
                {
                    Console.WriteLine($"Failed to notify host: {ex.Message}");
                    return MatchLobbyResultCode.Lobby_ConnectionError;
                }

                return MatchLobbyResultCode.Lobby_MatchLobbyCreated;

            }
            catch (TimeoutException ex)
            {
                Console.WriteLine($"Timeout creating the match: {ex.Message}");
                return MatchLobbyResultCode.Lobby_MatchLobbyCreationError;
            }
        }
    }

    internal class MatchLobby
    {
        public string matchCode {  get; set; }
        public List<LobbyPlayerDTO> players { get; set; }
        public IMatchLobbyManagerCallback matchLobbyCallback { get; set; }
    }
}
