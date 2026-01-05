using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Game;
using Contracts;
using Contracts.DTO;
using Contracts.DTO.Game_DTO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement
{
    public class GameNotifier : IGameNotifier
    {
        private readonly ILoggerHelper loggerHelper;

        private readonly ConcurrentDictionary<int, int> userFailureCounts = new ConcurrentDictionary<int, int>();
        private const int MaxFailuresBeforeKick = 3;

        public event Action<int> ClientConnectionLost;

        public GameNotifier(ILoggerHelper loggerHelper)
        {
            this.loggerHelper = loggerHelper ?? throw new ArgumentNullException(nameof(loggerHelper));
        }

        public void NotifyArchAddedToBoard(ArchAddedToBoardDTO data) => NotifyAll(cb => cb.OnArchAddedToBoard(data));
        public void NotifyArchArmyProvoked(ArchArmyProvokedDTO data) => NotifyAll(cb => cb.OnArchArmyProvoked(data));
        public void NotifyBattleResolved(BattleResultDTO data) => NotifyAll(cb => cb.OnBattleResolved(data));
        public void NotifyBodyPartAttached(BodyPartAttachedDTO data) => NotifyAll(cb => cb.OnBodyPartAttached(data));
        public void NotifyCardDrawn(CardDrawnDTO data) => NotifyAll(cb => cb.OnCardDrawn(data));
        public void NotifyDinoHeadPlayed(DinoPlayedDTO data) => NotifyAll(cb => cb.OnDinoHeadPlayed(data));
        public void NotifyGameEnded(GameEndedDTO data) => NotifyAll(cb => cb.OnGameEnded(data));
        public void NotifyGameInitialized(GameInitializedDTO data) => NotifyAll(cb => cb.OnGameInitialized(data));
        public void NotifyPlayerExpelled(PlayerExpelledDTO data) => NotifyAll(cb => cb.OnPlayerExpelled(data));
        public void NotifyTurnChanged(TurnChangedDTO data) => NotifyAll(cb => cb.OnTurnChanged(data));
        public void NotifyCardTakenFromDiscard(CardTakenFromDiscardDTO data) => NotifyAll(cb => cb.OnCardTakenFromDiscard(data));

        public void NotifyGameStarted(GameStartedDTO data)
        {
            if (data == null)
            {
                loggerHelper.LogWarning("NotifyGameStarted called with null data");
                throw new ArgumentNullException(nameof(data));
            }

            var callback = GameCallbackRegistry.Instance.GetCallback(data.MyUserId);

            if (callback == null)
            {
                loggerHelper.LogWarning($"No callback found for user {data.MyUserId} in NotifyGameStarted");
            }

            try
            {
                callback.OnGameStarted(data);
                loggerHelper.LogInfo($"Game started notification sent to user {data.MyUserId}");

                ResetFailureCount(data.MyUserId);
            }
            catch (Exception ex)
            {
                HandleCallbackFailure(data.MyUserId, callback, ex);
            }
        }

        private void NotifyAll(Action<IGameManagerCallback> action)
        {
            var registeredCallbacks = GameCallbackRegistry.Instance.GetRegisteredCallbacksSnapshot();

            foreach (var entry in registeredCallbacks)
            {
                int userId = entry.Key;
                var callback = entry.Value;

                try
                {
                    action(callback);

                    ResetFailureCount(userId);
                }
                catch (Exception ex)
                {
                    HandleCallbackFailure(userId, callback, ex);
                }
            }
        }

        private void ResetFailureCount(int userId)
        {
            if (userFailureCounts.ContainsKey(userId))
            {
                int ignored;
                userFailureCounts.TryRemove(userId, out ignored);
            }
        }

        private void HandleCallbackFailure(int userId, IGameManagerCallback callback, Exception ex)
        {
            if (!(ex is CommunicationException || ex is TimeoutException || ex is ObjectDisposedException))
            {
                loggerHelper.LogError($"Logic error in callback for user {userId}: {ex.Message}", ex);
                return;
            }

            int currentFailures = userFailureCounts.AddOrUpdate(userId, 1, (key, oldValue) => oldValue + 1);

            loggerHelper.LogWarning($"User {userId} callback failed ({ex.GetType().Name}). Count: {currentFailures}/{MaxFailuresBeforeKick}");

            if (currentFailures >= MaxFailuresBeforeKick)
            {
                loggerHelper.LogError($"User {userId} reached max failures. Removing callback and kicking from logic. {ex}", ex);

                GameCallbackRegistry.Instance.UnregisterCallback(userId);
                ResetFailureCount(userId);

                ClientConnectionLost?.Invoke(userId);
            }
        }
    }
}