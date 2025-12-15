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

        public void NotifyGameStarted(GameStartedDTO data) => NotifyAll(cb => cb.OnGameStarted(data));

        public void NotifyPlayerExpelled(PlayerExpelledDTO data) => NotifyAll(cb => cb.OnPlayerExpelled(data));

        public void NotifyTurnChanged(TurnChangedDTO data) => NotifyAll(cb => cb.OnTurnChanged(data));


        public void RegisterCallback()
        {
            try
            {
                IGameManagerCallback callback = OperationContext.Current.GetCallbackChannel<IGameManagerCallback>();
                lock (syncRoot)
                {
                    if (!activeCallbacks.Contains(callback))
                    {
                        activeCallbacks.Add(callback);
                        logger.LogInfo("New player callback registered.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error registering callback.", ex);
            }
        }

        public void UnregisterCallback(IGameManagerCallback callback)
        {
            if (callback == null) return;

            lock (syncRoot)
            {
                activeCallbacks.Remove(callback);
                logger.LogInfo("Player callback unregistered.");
            }
        }

        private void NotifyAll(Action<IGameManagerCallback> action)
        {
            List<IGameManagerCallback> failed = new List<IGameManagerCallback>();
            lock (syncRoot)
            {
                foreach (var cb in activeCallbacks)
                {
                    try { action(cb); }
                    catch
                    {
                        failed.Add(cb);
                    }
                }

                foreach (var f in failed) activeCallbacks.Remove(f);
            }
        }
    }
}
