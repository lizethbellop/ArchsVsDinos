using Contracts.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    [ServiceContract]
    public interface ILobbyManagerCallback
    {
        [OperationContract(IsOneWay = true)]
        void CreatedLobby(LobbyPlayerDTO hostLobbyPlayerDTO, string matchCode);
        
        [OperationContract(IsOneWay = true)]
        void JoinedLobby(LobbyPlayerDTO userAccountDTO);

        [OperationContract(IsOneWay = true)]
        void LobbyCancelled(string matchCode);

        /*
        [OperationContract]
        void InvitedFriendToLobby(string username, string matchCode);

        [OperationContract(IsOneWay = true)]
        void InvitedByEmailToLobby(string email, string matchCode);

        [OperationContract(IsOneWay = true)]
        void ExpelledPlayerFromLobby(LobbyPlayerDTO explledPlayer);

        [OperationContract(IsOneWay = true)]
        void LeftLobby(LobbyPlayerDTO playerWhoLeft);

        */

    }
}
