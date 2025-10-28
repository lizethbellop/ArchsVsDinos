using Contracts.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
   public interface IMatchLobbyManagerCallback
    {
        [OperationContract(IsOneWay = true)]
        void CreatedMatch(LobbyPlayerDTO hostLobbyPlayerDTO, string matchCode);

        /*
        [OperationContract(IsOneWay = true)]
        void JoinedMatch(LobbyPlayerDTO userAccountDTO);

        [OperationContract]
        void InvitedFriendToMatch(string username, string matchCode);

        [OperationContract(IsOneWay = true)]
        void InvitedByEmailToMatch(string email, string matchCode);

        [OperationContract(IsOneWay = true)]
        void ExpelledPlayerFromMatch(LobbyPlayerDTO explledPlayer);

        [OperationContract(IsOneWay = true)]
        void LeftMatchLobby(LobbyPlayerDTO playerWhoLeft);

        [OperationContract(IsOneWay = true)]
        void CancelledMatchLobby(string hostUsername);*/

    }
}
