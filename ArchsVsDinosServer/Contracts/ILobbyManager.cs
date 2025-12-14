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
        void ConnectToLobby(string lobbyCode, string nickname);

        [OperationContract]
        void DisconnectFromLobby(string lobbyCode, string nickname);

        [OperationContract]
        void SetReadyStatus(string lobbyCode, string nickname, bool isReady);

        [OperationContract]
        void StartGame(string lobbyCode);
    }

}
