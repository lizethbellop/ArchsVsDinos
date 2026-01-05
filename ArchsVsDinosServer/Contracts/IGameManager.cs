using Contracts.DTO.Game_DTO;
using Contracts.DTO.Game_DTO.Enums;
using Contracts.DTO.Game_DTO.State;
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

        /*
        [OperationContract]
        void DrawCard(string matchCode, int userId);

        [OperationContract]
        void PlayDinoHead(string matchCode, int userId, int cardId);

        [OperationContract]
        void AttachBodyPartToDino(string matchCode, int userId, AttachBodyPartDTO attachmentData);

        [OperationContract]
        void TakeCardFromDiscardPile(string matchCode, int userId, int cardId);

        [OperationContract]
        void ProvokeArchArmy(string matchCode, int userId, ArmyType armyType);

        [OperationContract]
        void EndTurn(string matchCode, int userId);*/

        [OperationContract]
        Task<DrawCardResultCode> DrawCard(string matchCode, int userId);

        [OperationContract]
        Task<PlayCardResultCode> PlayDinoHead(string matchCode, int userId, int cardId);

        [OperationContract]
        Task<PlayCardResultCode> AttachBodyPartToDino(string matchCode, int userId, AttachBodyPartDTO attachmentData);

        [OperationContract]
        Task<DrawCardResultCode> TakeCardFromDiscardPile(string matchCode, int userId, int cardId);

        [OperationContract]
        Task<ProvokeResultCode> ProvokeArchArmy(string matchCode, int userId, ArmyType armyType);

        [OperationContract]
        Task<EndTurnResultCode> EndTurn(string matchCode, int userId);
    }

}
