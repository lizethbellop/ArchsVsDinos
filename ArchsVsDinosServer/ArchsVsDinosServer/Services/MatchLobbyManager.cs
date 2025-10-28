using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement;
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
    public class MatchLobbyManager : IMatchLobbyManager
    {

        private LobbyConfiguration lobbyBusinessLogic;

        public MatchLobbyManager()
        {
            lobbyBusinessLogic = new LobbyConfiguration();
        }

        public MatchLobbyResultCode CreateMatch(UserAccountDTO hostUserAccountDTO)
        {
            return lobbyBusinessLogic.CreateANewMatch(hostUserAccountDTO);
        }

        /*
        public JoinMatch(UserAccountDTO userAccountDTO, string matchCode)
        {

        }

        public InviteFriendToMatch(string username, string friendUsername, string matchCode)
        {

        }

        public InviteByEmailToMatch(string email, string matchCode)
        {

        }

        public ExpelPlayerFromMatch(string username)
        {

        }

        public LeaveMatchLobby(string username)
        {

        }

        public CancelMatchLobby(string username)
        {

        }*/

    }
}
