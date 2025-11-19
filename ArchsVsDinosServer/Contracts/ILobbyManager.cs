using Contracts.DTO;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    [ServiceContract(CallbackContract = typeof(ILobbyManagerCallback))]
    public interface ILobbyManager
    {
        [OperationContract]
        LobbyResultCode CreateLobby(UserAccountDTO hostUserAccountDTO);


        [OperationContract]
        LobbyResultCode JoinLobby(UserAccountDTO userAccountDTO, string matchCode);

        [OperationContract]
        LobbyResultCode CancelLobby(string matchCode, string usernameRequester);

        /*

        [OperationContract]
        void InviteFriendToLobby(string username, string friendUsername, string matchCode);

        [OperationContract]
        void InviteByEmailToLobby(string email, string matchCode);

        [OperationContract]
        void ExpelPlayerFromLobby(string username);

        [OperationContract]
        void LeaveLobby(string username);

*/

    }

}
