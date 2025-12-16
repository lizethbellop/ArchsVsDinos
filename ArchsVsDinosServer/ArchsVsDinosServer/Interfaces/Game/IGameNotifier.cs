using Contracts;
using Contracts.DTO;
using Contracts.DTO.Game_DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Interfaces.Game
{
    public interface IGameNotifier
    {
        void NotifyGameInitialized(GameInitializedDTO data);
        void NotifyGameStarted(GameStartedDTO data);
        void NotifyTurnChanged(TurnChangedDTO data);
        void NotifyCardDrawn(CardDrawnDTO data);
        void NotifyDinoHeadPlayed(DinoPlayedDTO data);
        void NotifyBodyPartAttached(BodyPartAttachedDTO data);
        void NotifyArchAddedToBoard(ArchAddedToBoardDTO data);
        void NotifyArchArmyProvoked(ArchArmyProvokedDTO data);
        void NotifyBattleResolved(BattleResultDTO data);
        void NotifyGameEnded(GameEndedDTO data);
        void NotifyPlayerExpelled(PlayerExpelledDTO data);

        void NotifyCardExchanged(CardExchangedDTO data);
    }

}
