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

        [OperationContract]
        LobbyResultCode LeaveLobby(string username);

        [OperationContract]
        LobbyResultCode ExpelPlayerLobby(string username, string hostUsername);

        /*

        [OperationContract]
        void InviteFriendToLobby(string username, string friendUsername, string matchCode);

        [OperationContract]
        void InviteByEmailToLobby(string email, string matchCode);

        */

    }

}
