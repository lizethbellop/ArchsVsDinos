using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Game;
using Contracts;
using Contracts.DTO;
using Contracts.DTO.Game_DTO;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement
{
    public sealed class GameNotifier : IGameNotifier
    {
        private const int MAX_FAILURES_BEFORE_KICK = 3;

        private readonly ILoggerHelper loggerHelper;
        private readonly GameSessionManager sessions;
        private readonly Func<IGameLogic> gameLogicProvider;

        private readonly ConcurrentDictionary<int, int> userFailureCounts;

        public event Action<int> ClientConnectionLost;

        public GameNotifier(
            ILoggerHelper loggerHelper,
            GameSessionManager sessions,
            Func<IGameLogic> gameLogicProvider)
        {
            this.loggerHelper = loggerHelper ?? throw new ArgumentNullException(nameof(loggerHelper));
            this.sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            this.gameLogicProvider = gameLogicProvider ?? throw new ArgumentNullException(nameof(gameLogicProvider));

            userFailureCounts = new ConcurrentDictionary<int, int>();
        }

        public void NotifyArchAddedToBoard(ArchAddedToBoardDTO data) => NotifyAll(cb => cb.OnArchAddedToBoard(data));
        public void NotifyArchArmyProvoked(ArchArmyProvokedDTO data) => NotifyAll(cb => cb.OnArchArmyProvoked(data));
        public void NotifyBattleResolved(BattleResultDTO data) => NotifyAll(cb => cb.OnBattleResolved(data));
        public void NotifyBodyPartAttached(BodyPartAttachedDTO data) => NotifyAll(cb => cb.OnBodyPartAttached(data));
        public void NotifyCardDrawn(CardDrawnDTO data) => NotifyAll(cb => cb.OnCardDrawn(data));
        public void NotifyCardTakenFromDiscard(CardTakenFromDiscardDTO data) => NotifyAll(cb => cb.OnCardTakenFromDiscard(data));
        public void NotifyDinoHeadPlayed(DinoPlayedDTO data) => NotifyAll(cb => cb.OnDinoHeadPlayed(data));
        public void NotifyGameEnded(GameEndedDTO data) => NotifyAll(cb => cb.OnGameEnded(data));
        public void NotifyGameInitialized(GameInitializedDTO data) => NotifyAll(cb => cb.OnGameInitialized(data));
        public void NotifyPlayerExpelled(PlayerExpelledDTO data) => NotifyAll(cb => cb.OnPlayerExpelled(data));
        public void NotifyTurnChanged(TurnChangedDTO data) => NotifyAll(cb => cb.OnTurnChanged(data));

        public void NotifyGameStarted(GameStartedDTO data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            IGameManagerCallback callback = GameCallbackRegistry.Instance.GetCallback(data.MyUserId);
            if (callback == null)
            {
                loggerHelper.LogWarning(string.Format(
                    "No callback found for user {0} in NotifyGameStarted.",
                    data.MyUserId));

                return;
            }

            try
            {
                callback.OnGameStarted(data);
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
                IGameManagerCallback callback = entry.Value;

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
            if (userId <= 0)
            {
                return;
            }

            userFailureCounts.TryRemove(userId, out int ignored);
        }

        private void HandleCallbackFailure(int userId, IGameManagerCallback callback, Exception ex)
        {
            if (userId <= 0)
            {
                return;
            }

            if (!IsTransportCallbackException(ex))
            {
                loggerHelper.LogError(string.Format("Logic error in callback for user {0}", userId), ex);
                return;
            }

            int currentFailures = userFailureCounts.AddOrUpdate(userId, 1, (key, oldValue) => oldValue + 1);

            loggerHelper.LogWarning(string.Format(
                "User {0} callback failed ({1}). Count: {2}/{3}",
                userId,
                ex.GetType().Name,
                currentFailures,
                MAX_FAILURES_BEFORE_KICK));

            if (currentFailures < MAX_FAILURES_BEFORE_KICK)
            {
                return;
            }

            HandleMaxFailures(userId, ex);
        }

        private void HandleMaxFailures(int userId, Exception ex)
        {
            string matchCode;
            GameCallbackRegistry.Instance.TryGetMatchCode(userId, out matchCode);

            loggerHelper.LogError(string.Format(
                "User {0} reached max failures. match={1}. Removing callback and kicking.",
                userId,
                matchCode), ex);

            GameCallbackRegistry.Instance.UnregisterPlayer(userId);
            ResetFailureCount(userId);

            if (!string.IsNullOrWhiteSpace(matchCode))
            {
                try
                {
                    IGameLogic logic = gameLogicProvider.Invoke();
                    if (logic != null)
                    {
                        logic.LeaveGame(matchCode, userId);
                    }
                }
                catch (Exception leaveEx)
                {
                    loggerHelper.LogWarning(string.Format(
                        "LeaveGame failed for user {0} match {1}: {2}",
                        userId,
                        matchCode,
                        leaveEx.Message));
                }
            }

            ClientConnectionLost?.Invoke(userId);
        }

        private static bool IsTransportCallbackException(Exception ex)
        {
            return ex is CommunicationException ||
                   ex is TimeoutException ||
                   ex is ObjectDisposedException;
        }
    }
}