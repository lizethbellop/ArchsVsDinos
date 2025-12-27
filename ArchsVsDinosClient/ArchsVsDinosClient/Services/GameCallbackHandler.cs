using ArchsVsDinosClient.GameService;
using System;
using System.ServiceModel;

namespace ArchsVsDinosClient.Services
{
    public class GameCallbackHandler : IGameManagerCallback
    {
        public event Action<GameInitializedDTO> OnGameInitializedEvent;
        public event Action<GameStartedDTO> OnGameStartedEvent;
        public event Action<TurnChangedDTO> OnTurnChangedEvent;
        public event Action<CardDrawnDTO> OnCardDrawnEvent;
        public event Action<DinoPlayedDTO> OnDinoPlayedEvent;
        public event Action<BodyPartAttachedDTO> OnBodyPartAttachedEvent;
        public event Action<ArchAddedToBoardDTO> OnArchAddedEvent;
        public event Action<ArchArmyProvokedDTO> OnArchProvokedEvent;
        public event Action<BattleResultDTO> OnBattleResolvedEvent;
        public event Action<GameEndedDTO> OnGameEndedEvent;
        public event Action<PlayerExpelledDTO> OnPlayerExpelledEvent;
        public event Action<CardExchangedDTO> OnCardExchangedEvent;

        public void OnGameInitialized(GameInitializedDTO data)
        {
            OnGameInitializedEvent?.Invoke(data);
        }

        public void OnGameStarted(GameStartedDTO data)
        {
            OnGameStartedEvent?.Invoke(data);
        }

        public void OnTurnChanged(TurnChangedDTO data)
        {
            OnTurnChangedEvent?.Invoke(data);
        }

        public void OnCardDrawn(CardDrawnDTO data)
        {
            OnCardDrawnEvent?.Invoke(data);
        }

        public void OnDinoHeadPlayed(DinoPlayedDTO data)
        {
            OnDinoPlayedEvent?.Invoke(data);
        }

        public void OnBodyPartAttached(BodyPartAttachedDTO data)
        {
            OnBodyPartAttachedEvent?.Invoke(data);
        }

        public void OnArchAddedToBoard(ArchAddedToBoardDTO data)
        {
            OnArchAddedEvent?.Invoke(data);
        }

        public void OnArchArmyProvoked(ArchArmyProvokedDTO data)
        {
            OnArchProvokedEvent?.Invoke(data);
        }

        public void OnBattleResolved(BattleResultDTO data)
        {
            OnBattleResolvedEvent?.Invoke(data);
        }

        public void OnGameEnded(GameEndedDTO data)
        {
            OnGameEndedEvent?.Invoke(data);
        }

        public void OnPlayerExpelled(PlayerExpelledDTO dto)
        {
            OnPlayerExpelledEvent?.Invoke(dto);
        }

        public void OnCardExchanged(CardExchangedDTO dto)
        {
            OnCardExchangedEvent?.Invoke(dto);
        }
    }
}