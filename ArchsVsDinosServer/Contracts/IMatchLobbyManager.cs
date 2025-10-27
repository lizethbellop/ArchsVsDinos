using Contracts.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    [ServiceContract(CallbackContract = typeof(IMatchLobbyManagerCallback))]
    public interface IMatchLobbyManager
    {
        [OperationContract]
        void CreateMatch(UserAccountDTO hostUserAccountDTO);

        [OperationContract]
        void JoinMatch(UserAccountDTO userAccountDTO, string matchCode);

        [OperationContract]
        void InviteFriendToMatch(string username, string friendUsername, string matchCode);

        [OperationContract]
        void InviteByEmailToMatch(string email, string matchCode);

        [OperationContract]
        void ExpelPlayerFromMatch(string username);

        [OperationContract]
        void LeaveMatchLobby(string username);

        [OperationContract]
        void CancelMatchLobby(string username);
    
    }

}
