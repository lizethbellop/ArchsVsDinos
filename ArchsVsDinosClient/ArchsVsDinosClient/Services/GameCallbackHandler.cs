using ArchsVsDinosClient.GameService;
using ArchsVsDinosClient.Utils;
using System;
using System.ServiceModel;

namespace ArchsVsDinosClient.Services
{
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, UseSynchronizationContext = false)]
    public class GameCallbackHandler : IGameManagerCallback
    {
        private GameConnectionTimer connectionTimer;

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
        public event Action<CardTakenFromDiscardDTO> OnCardTakenFromDiscardEvent;

        /// <summary>
        /// Establece el timer que se reseteará en cada callback del servidor
        /// </summary>
        public void SetConnectionTimer(GameConnectionTimer timer)
        {
            this.connectionTimer = timer;
        }

        public void OnGameInitialized(GameInitializedDTO data)
        {
            connectionTimer?.Reset();
            OnGameInitializedEvent?.Invoke(data);
        }

        public void OnGameStarted(GameStartedDTO data)
        {
            connectionTimer?.Reset();
            OnGameStartedEvent?.Invoke(data);
        }

        public void OnTurnChanged(TurnChangedDTO data)
        {
            connectionTimer?.Reset();
            OnTurnChangedEvent?.Invoke(data);
        }

        public void OnCardDrawn(CardDrawnDTO data)
        {
            connectionTimer?.Reset();
            OnCardDrawnEvent?.Invoke(data);
        }

        public void OnDinoHeadPlayed(DinoPlayedDTO data)
        {
            connectionTimer?.Reset();
            OnDinoPlayedEvent?.Invoke(data);
        }

        public void OnBodyPartAttached(BodyPartAttachedDTO data)
        {
            connectionTimer?.Reset();
            OnBodyPartAttachedEvent?.Invoke(data);
        }

        public void OnArchAddedToBoard(ArchAddedToBoardDTO data)
        {
            connectionTimer?.Reset();
            OnArchAddedEvent?.Invoke(data);
        }

        public void OnArchArmyProvoked(ArchArmyProvokedDTO data)
        {
            connectionTimer?.Reset();
            OnArchProvokedEvent?.Invoke(data);
        }

        public void OnBattleResolved(BattleResultDTO data)
        {
            connectionTimer?.Reset();
            OnBattleResolvedEvent?.Invoke(data);
        }

        public void OnGameEnded(GameEndedDTO data)
        {
            connectionTimer?.Reset();
            OnGameEndedEvent?.Invoke(data);
        }

        public void OnPlayerExpelled(PlayerExpelledDTO dto)
        {
            connectionTimer?.Reset();
            OnPlayerExpelledEvent?.Invoke(dto);
        }

        public void OnCardTakenFromDiscard(CardTakenFromDiscardDTO data)
        {
            connectionTimer?.Reset();
            OnCardTakenFromDiscardEvent?.Invoke(data);
        }
    }
}