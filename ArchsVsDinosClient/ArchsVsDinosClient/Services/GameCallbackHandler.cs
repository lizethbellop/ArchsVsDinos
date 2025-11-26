using ArchsVsDinosClient.GameService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services
{
    public class GameCallbackHandler : IGameManagerCallback
    {
        public event Action<GameInitializedDTO> GameInitialized;
        public event Action<GameStartedDTO> GameStarted;
        public event Action<TurnChangedDTO> TurnChanged;
        public event Action<CardDrawnDTO> CardDrawn;
        public event Action<DinoPlayedDTO> DinoHeadPlayed;
        public event Action<BodyPartAttachedDTO> BodyPartAttached;
        public event Action<ArchAddedToBoardDTO> ArchAddedToBoard;
        public event Action<ArchArmyProvokedDTO> ArchArmyProvoked;
        public event Action<BattleResultDTO> BattleResolved;
        public event Action<GameEndedDTO> GameEnded;

        public event Action<PlayerExpelledDTO> PlayerExpelled;

        public void OnGameInitialized(GameInitializedDTO data)
        {
            GameInitialized?.Invoke(data);
        }

        public void OnGameStarted(GameStartedDTO data)
        {
            GameStarted?.Invoke(data);
        }

        public void OnTurnChanged(TurnChangedDTO data)
        {
            TurnChanged?.Invoke(data);
        }

        public void OnCardDrawn(CardDrawnDTO data)
        {
            CardDrawn?.Invoke(data);
        }

        public void OnDinoHeadPlayed(DinoPlayedDTO data)
        {
            DinoHeadPlayed?.Invoke(data);
        }

        public void OnBodyPartAttached(BodyPartAttachedDTO data)
        {
            BodyPartAttached?.Invoke(data);
        }

        public void OnArchAddedToBoard(ArchAddedToBoardDTO data)
        {
            ArchAddedToBoard?.Invoke(data);
        }

        public void OnArchArmyProvoked(ArchArmyProvokedDTO data)
        {
            ArchArmyProvoked?.Invoke(data);
        }

        public void OnBattleResolved(BattleResultDTO data)
        {
            BattleResolved?.Invoke(data);
        }

        public void OnGameEnded(GameEndedDTO data)
        {
            GameEnded?.Invoke(data);
        }

        public void OnPlayerExpelled(PlayerExpelledDTO data)
        {
            PlayerExpelled?.Invoke(data);
        }
    }
}
