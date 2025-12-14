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
        void PlayerJoinedLobby(string nickname);

        [OperationContract(IsOneWay = true)]
        void PlayerLeftLobby(string nickname);

        [OperationContract(IsOneWay = true)]
        void UpdateListOfPlayers(LobbyPlayerDTO[] players);

        [OperationContract(IsOneWay = true)]
        void PlayerReadyStatusChanged(string nickname, bool isReady);

        [OperationContract(IsOneWay = true)]
        void GameStarting();

    }
}
