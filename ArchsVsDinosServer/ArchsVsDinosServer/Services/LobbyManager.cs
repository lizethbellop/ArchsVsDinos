using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Services.Interfaces;
using Contracts;
using Contracts.DTO;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class LobbyManager : ILobbyManager, ILobbyNotifier
    {

        private readonly LobbyConfiguration lobbyBusinessLogic;

        public LobbyManager()
        {
            lobbyBusinessLogic = new LobbyConfiguration();
        }

        
        public LobbyResultCode CreateLobby(UserAccountDTO hostUserAccountDTO)
        {
            return lobbyBusinessLogic.CreateANewMatch(hostUserAccountDTO);
        }

        
        public LobbyResultCode JoinLobby(UserAccountDTO userAccountDTO, string matchCode)
        {
            return lobbyBusinessLogic.JoinToTheLobbyWithCode(userAccountDTO, matchCode);
        }

        public LobbyResultCode CancelLobby(string matchCode, string usernameRequester)
        {
            return lobbyBusinessLogic.CancelTheLobby(matchCode, usernameRequester);
        }

        public LobbyResultCode LeaveLobby(string username)
        {
            return lobbyBusinessLogic.LeaveTheLobby(username);
        }

        public LobbyResultCode ExpelPlayerLobby(string username, string hostUsername)
        {
            return lobbyBusinessLogic.ExpelThePlayer(username, hostUsername);
        }

        public void NotifyPlayerExpelled(string username, string reason)
        {
            throw new NotImplementedException();
        }

        public void NotifyLobbyClosure(string reason)
        {
            throw new NotImplementedException();
        }
        /*
public InviteFriendToMatch(string username, string friendUsername, string matchCode)
{

}

public InviteByEmailToMatch(string email, string matchCode)
{

}
*/
        /*


        public LeaveMatchLobby(string username)
        {

        }
        */


    }
}
