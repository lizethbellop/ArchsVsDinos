using Contracts.DTO.Game_DTO;
using Contracts.DTO.Game_DTO.State;
using Contracts.DTO.Game_DTO.Swap;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    [ServiceContract(CallbackContract = typeof(IGameManagerCallback), SessionMode = SessionMode.Required)]
    public interface IGameManager
    {
        [OperationContract]
        void ConnectToGame(string matchCode, int userId);

        [OperationContract]
        void LeaveGame(string matchCode, int userId);

        [OperationContract]
        GameSetupResultCode InitializeGame(string matchCode);

        [OperationContract]
        DrawCardResultCode DrawCard(string matchCode, int userId, int drawPileNumber);

        [OperationContract]
        PlayCardResultCode PlayDinoHead(string matchCode, int userId, int cardId);

        [OperationContract]
        PlayCardResultCode AttachBodyPartToDino(string matchCode, int userId, AttachBodyPartDTO attachmentData);

        [OperationContract]
        SwapCardResultCode SwapCardWithPlayer(string matchCode, int initiatorUserId, SwapCardRequestDTO request);

        [OperationContract]
        ProvokeResultCode ProvokeArchArmy(string matchCode, int userId, string armyType);

        [OperationContract]
        EndTurnResultCode EndTurn(string matchCode, int userId);

        [OperationContract]
        GameStateDTO GetGameState(string matchCode);

        [OperationContract]
        PlayerHandDTO GetPlayerHand(string matchCode, int userId);

        [OperationContract]
        CentralBoardDTO GetCentralBoard(string matchCode);
    }
}
