using ArchsVsDinosServer.Utils;
using Contracts;
using Contracts.DTO;
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

        //private readonly ConcurrentDictionary<string, MatchLobby> ActiveMatches = new();

        public void CreateANewMatch(UserAccountDTO hostUserAccountDTO)
        {

            /*   IMatchLobbyManagerCallback callback = null;

               try
               {
                   callback = OperationContext.Current.GetCallbackChannel<IMatchLobbyManagerCallback>();
               }
               catch (InvalidOperationException ex)
               {
                   Console.WriteLine($"Error obtaining the callback: {ex.Message}");
                   throw new FaultException("A communication channel could not be estalished");
               }

               try
               {
                   string matchCode = CodeGenerator.GenerateMatchCode();

                   LobbyPlayerDTO lobbyHostPlayer = new LobbyPlayerDTO
                   {
                       username = hostUserAccountDTO.username,
                       nickname = hostUserAccountDTO.nickname,
                       profilePicture = null,
                       isHost = true
                   };

                   MatchLobby matchLobby = new MatchLobby
                   {
                       matchCode = matchCode,
                       players = new List<LobbyPlayerDTO> { lobbyHostPlayer },
                       matchLobbyCallback = callback
                   };

                   ActiveMatches.TryAdd(matchCode, matchLobby);
                   callback.CreatedMatch(hostLobbyPlayer, matchCode);
               }
               catch (TimeoutException ex)
               {
                   Console.WriteLine($"Timeout creating the match {MatchLobby}: {ex.Message}");
                   NotifySender(username, $"Timeout al enviar mensaje a '{targetUser}'");
               }*/
        }
    }

    internal class MatchLobby
    {
        public string matchCode {  get; set; }
        public List<LobbyPlayerDTO> players { get; set; }
        public IMatchLobbyManagerCallback matchLobbyCallback { get; set; }
    }
}
