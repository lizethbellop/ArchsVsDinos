using Contracts.DTO;
using Contracts.DTO.Game_DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    [ServiceContract]
    public interface IGameManagerCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnGameInitialized(GameInitializedDTO data);

        [OperationContract(IsOneWay = true)]
        void OnGameStarted(GameStartedDTO data);

        [OperationContract(IsOneWay = true)]
        void OnTurnChanged(TurnChangedDTO data);

        [OperationContract(IsOneWay = true)]
        void OnCardDrawn(CardDrawnDTO data);

        [OperationContract(IsOneWay = true)]
        void OnDinoHeadPlayed(DinoPlayedDTO data);

        [OperationContract(IsOneWay = true)]
        void OnBodyPartAttached(BodyPartAttachedDTO data);

        [OperationContract(IsOneWay = true)]
        void OnArchAddedToBoard(ArchAddedToBoardDTO data);

        [OperationContract(IsOneWay = true)]
        void OnArchArmyProvoked(ArchArmyProvokedDTO data);

        [OperationContract(IsOneWay = true)]
        void OnBattleResolved(BattleResultDTO data);

        [OperationContract(IsOneWay = true)]
        void OnGameEnded(GameEndedDTO data);

        [OperationContract(IsOneWay = true)]
        void OnPlayerExpelled(PlayerExpelledDTO dto);

        [OperationContract(IsOneWay = true)]
        void OnCardTakenFromDiscard(CardTakenFromDiscardDTO data);

        [OperationContract(IsOneWay = true)]
        void OnCardExchanged(CardExchangedDTO dto);
    }
}
