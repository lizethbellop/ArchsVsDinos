using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Contracts.DTO.Result_Codes;
using Contracts.DTO.Game_DTO.State;
using Contracts.DTO.Game_DTO;

namespace Contracts
{
    [ServiceContract(CallbackContract = typeof(IGameManagerCallback))]
    public interface IGameManager
    {
        [OperationContract]
        GameSetupResultCode InitializeGame(int matchId);

        [OperationContract]
        GameSetupResultCode StartGame(int matchId);

        [OperationContract]
        DrawCardResultCode DrawCard(int matchId, int userId, int drawPileNumber);

        [OperationContract]
        PlayCardResultCode PlayDinoHead(int matchId, int userId, int cardId);

        [OperationContract]
        PlayCardResultCode AttachBodyPartToDino(int matchId, int userId, int cardId, int dinoHeadCardId);

        [OperationContract]
        ProvokeResultCode ProvokeArchArmy(int matchId, int userId, string armyType);

        [OperationContract]
        EndTurnResultCode EndTurn(int matchId, int userId);

        [OperationContract]
        GameStateDTO GetGameState(int matchId);

        [OperationContract]
        PlayerHandDTO GetPlayerHand(int matchId, int userId);

        [OperationContract]
        CentralBoardDTO GetCentralBoard(int matchId);
    }
}
