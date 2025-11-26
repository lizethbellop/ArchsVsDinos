using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Contracts.DTO;

namespace Contracts
{
    [ServiceContract]
    public interface IChatManagerCallback
    {
        [OperationContract(IsOneWay = true)]
        void ReceiveMessage(string roomId, string fromUser, string message);

        [OperationContract(IsOneWay = true)]
        void ReceiveSystemNotification(ChatResultCode code, string notification);

        [OperationContract(IsOneWay = true)]
        void UpdateUserList(List<string> users);

        [OperationContract(IsOneWay = true)]
        void UserBannedFromChat(string username, int strikes);

        [OperationContract(IsOneWay = true)]
        void UserExpelledFromLobby(string username, string reason);

        [OperationContract(IsOneWay = true)]
        void LobbyClosedDueToInsufficientPlayers(string reason);

    }
}
