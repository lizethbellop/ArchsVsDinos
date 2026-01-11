using ArchsVsDinosClient.GameService;
using ArchsVsDinosClient.Utils;
using System;
using System.Diagnostics;
using System.ServiceModel;

namespace ArchsVsDinosClient.Services
{
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, UseSynchronizationContext = false)]
    public sealed class GameCallbackHandler : IGameManagerCallback
    {
        private const string CallbackLogPrefix = "[GAME CALLBACK]";

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

        public void SetConnectionTimer(GameConnectionTimer timer)
        {
            connectionTimer = timer;
            MarkActivity();
        }

        public void OnGameInitialized(GameInitializedDTO data)
        {
            MarkActivity();
            SafeInvoke(() => OnGameInitializedEvent?.Invoke(data), nameof(OnGameInitialized));
        }

        public void OnGameStarted(GameStartedDTO data)
        {
            MarkActivity();
            SafeInvoke(() => OnGameStartedEvent?.Invoke(data), nameof(OnGameStarted));
        }

        public void OnTurnChanged(TurnChangedDTO data)
        {
            MarkActivity();
            SafeInvoke(() => OnTurnChangedEvent?.Invoke(data), nameof(OnTurnChanged));
        }

        public void OnCardDrawn(CardDrawnDTO data)
        {
            MarkActivity();
            SafeInvoke(() => OnCardDrawnEvent?.Invoke(data), nameof(OnCardDrawn));
        }

        public void OnDinoHeadPlayed(DinoPlayedDTO data)
        {
            MarkActivity();
            SafeInvoke(() => OnDinoPlayedEvent?.Invoke(data), nameof(OnDinoHeadPlayed));
        }

        public void OnBodyPartAttached(BodyPartAttachedDTO data)
        {
            MarkActivity();
            SafeInvoke(() => OnBodyPartAttachedEvent?.Invoke(data), nameof(OnBodyPartAttached));
        }

        public void OnArchAddedToBoard(ArchAddedToBoardDTO data)
        {
            MarkActivity();
            SafeInvoke(() => OnArchAddedEvent?.Invoke(data), nameof(OnArchAddedToBoard));
        }

        public void OnArchArmyProvoked(ArchArmyProvokedDTO data)
        {
            MarkActivity();
            SafeInvoke(() => OnArchProvokedEvent?.Invoke(data), nameof(OnArchArmyProvoked));
        }

        public void OnBattleResolved(BattleResultDTO data)
        {
            MarkActivity();
            SafeInvoke(() => OnBattleResolvedEvent?.Invoke(data), nameof(OnBattleResolved));
        }

        public void OnGameEnded(GameEndedDTO data)
        {
            MarkActivity();
            SafeInvoke(() => OnGameEndedEvent?.Invoke(data), nameof(OnGameEnded));
        }

        public void OnPlayerExpelled(PlayerExpelledDTO dto)
        {
            MarkActivity();
            SafeInvoke(() => OnPlayerExpelledEvent?.Invoke(dto), nameof(OnPlayerExpelled));
        }

        public void OnCardTakenFromDiscard(CardTakenFromDiscardDTO data)
        {
            MarkActivity();
            SafeInvoke(() => OnCardTakenFromDiscardEvent?.Invoke(data), nameof(OnCardTakenFromDiscard));
        }

        private void MarkActivity()
        {
            connectionTimer?.NotifyActivity();
        }

        private void SafeInvoke(Action action, string methodName)
        {
            try
            {
                action?.Invoke();
            }
            catch (CommunicationException ex)
            {
                Debug.WriteLine($"{CallbackLogPrefix} CommunicationException in {methodName}: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine($"{CallbackLogPrefix} TimeoutException in {methodName}: {ex.Message}");
            }
            catch (ObjectDisposedException ex)
            {
                Debug.WriteLine($"{CallbackLogPrefix} ObjectDisposedException in {methodName}: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"{CallbackLogPrefix} InvalidOperationException in {methodName}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{CallbackLogPrefix} Unexpected exception in {methodName}: {ex.Message}");
            }
        }
    }
}