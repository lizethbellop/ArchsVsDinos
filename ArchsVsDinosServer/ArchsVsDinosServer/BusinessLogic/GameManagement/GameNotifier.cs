using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Game;
using Contracts;
using Contracts.DTO;
using Contracts.DTO.Game_DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement
{
    public class GameNotifier : IGameNotifier
    {
        private readonly List<IGameManagerCallback> activeCallbacks = new List<IGameManagerCallback>();
        private readonly object syncRoot = new object();
        private readonly ILoggerHelper logger;

        public GameNotifier(ILoggerHelper loggerHelper)
        {
            this.logger = loggerHelper ?? throw new ArgumentNullException(nameof(loggerHelper));
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
                logger.LogWarning("NotifyGameStarted called with null data");
                throw new ArgumentNullException(nameof(data));
            }

            var callback = GameCallbackRegistry.Instance.GetCallback(data.MyUserId);

            if (callback == null)
            {
                logger.LogWarning($"No callback found for user {data.MyUserId} in NotifyGameStarted");
                throw new InvalidOperationException($"No callback registered for user {data.MyUserId}");
            }

            try
            {
                callback.OnGameStarted(data);
                logger.LogInfo($"Game started notification sent to user {data.MyUserId}");
            }
            catch (CommunicationException ex)
            {
                logger.LogWarning($"Callback communication error for user {data.MyUserId}. Removing callback. - {ex.Message}");
                GameCallbackRegistry.Instance.UnregisterCallback(data.MyUserId);
                throw;
            }
            catch (ObjectDisposedException ex)
            {
                logger.LogWarning($"Callback disposed for user {data.MyUserId}. Removing callback. - {ex.Message}");
                GameCallbackRegistry.Instance.UnregisterCallback(data.MyUserId);
                throw;
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning($"Callback timeout for user {data.MyUserId}. Removing callback. - {ex.Message}");
                GameCallbackRegistry.Instance.UnregisterCallback(data.MyUserId);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError($"Invalid operation in callback for user {data.MyUserId}.", ex);
                GameCallbackRegistry.Instance.UnregisterCallback(data.MyUserId);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError($"Unexpected error while notifying user {data.MyUserId}.", ex);
                throw;
            }
        }

        private void NotifyAll(Action<IGameManagerCallback> action)
        {
            var failedCallbacks = new List<IGameManagerCallback>();

            foreach (var callback in GameCallbackRegistry.Instance.GetAllCallbacks())
            {
                try
                {
                    action(callback);
                }
                catch (CommunicationException ex)
                {
                    logger.LogWarning($"Callback communication error. Removing callback. - {ex.Message}");
                    failedCallbacks.Add(callback);
                }
                catch (ObjectDisposedException ex)
                {
                    logger.LogWarning($"Callback disposed. Removing callback. - {ex.Message}");
                    failedCallbacks.Add(callback);
                }
                catch (TimeoutException ex)
                {
                    logger.LogWarning($"Callback timeout. Removing callback. - {ex.Message}");
                    failedCallbacks.Add(callback);
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogError("Invalid operation in callback.", ex);
                    failedCallbacks.Add(callback);
                }
                catch (Exception ex)
                {
                    logger.LogError("Unexpected error while notifying callback.", ex);
                }
            }

            foreach (var cb in failedCallbacks)
            {
                RemoveCallback(cb);
            }
        }

        private void RemoveCallback(IGameManagerCallback callback)
        {
            if (callback == null) return;

            GameCallbackRegistry.Instance.UnregisterCallback(callback);
            logger.LogInfo("Callback removed due to failure.");
        }

    }
}
