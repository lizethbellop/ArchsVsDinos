using ArchsVsDinosServer.Interfaces;
using Contracts;
using Contracts.DTO.Game_DTO;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace ArchsVsDinosServer.Services
{
    public sealed class GameCallbackManager
    {
        private readonly List<IGameManagerCallback> activeCallbacks;
        private readonly object syncRoot;
        private readonly ILoggerHelper loggerHelper;

        private const string LOG_REGISTER_FAILED = "Failed to register callback.";
        private const string LOG_UNREGISTER_FAILED = "Failed to unregister callback.";
        private const string LOG_NOTIFY_FAILED = "Failed notifying a player.";

        public GameCallbackManager(ILoggerHelper loggerHelper)
        {
            this.loggerHelper = loggerHelper ?? throw new ArgumentNullException(nameof(loggerHelper));

            activeCallbacks = new List<IGameManagerCallback>();
            syncRoot = new object();
        }

        public void RegisterCallback()
        {
            try
            {
                if (OperationContext.Current == null)
                {
                    loggerHelper.LogWarning("RegisterCallback called without OperationContext.");
                    return;
                }

                IGameManagerCallback callback =
                    OperationContext.Current.GetCallbackChannel<IGameManagerCallback>();

                if (callback == null)
                {
                    loggerHelper.LogWarning("RegisterCallback received null callback.");
                    return;
                }

                lock (syncRoot)
                {
                    if (!activeCallbacks.Contains(callback))
                    {
                        activeCallbacks.Add(callback);
                        loggerHelper.LogInfo("New player callback registered.");
                    }
                }
            }
            catch (Exception ex)
            {
                loggerHelper.LogError(LOG_REGISTER_FAILED, ex);
            }
        }

        public void UnregisterCallback(IGameManagerCallback callback)
        {
            try
            {
                if (callback == null)
                {
                    return;
                }

                lock (syncRoot)
                {
                    activeCallbacks.Remove(callback);
                }

                loggerHelper.LogInfo("Player callback unregistered.");
            }
            catch (Exception ex)
            {
                loggerHelper.LogError(LOG_UNREGISTER_FAILED, ex);
            }
        }

        private void NotifyAll(Action<IGameManagerCallback> action)
        {
            List<IGameManagerCallback> failedCallbacks = new List<IGameManagerCallback>();

            lock (syncRoot)
            {
                foreach (IGameManagerCallback callback in activeCallbacks)
                {
                    try
                    {
                        action(callback);
                    }
                    catch (Exception ex)
                    {
                        failedCallbacks.Add(callback);
                        loggerHelper.LogError(LOG_NOTIFY_FAILED, ex);
                    }
                }

                foreach (IGameManagerCallback failed in failedCallbacks)
                {
                    activeCallbacks.Remove(failed);
                    loggerHelper.LogWarning("Removed a failed player callback from active list.");
                }
            }
        }

        public void NotifyGameInitialized(GameInitializedDTO gameData) => NotifyAll(cb => cb.OnGameInitialized(gameData));
        public void NotifyGameStarted(GameStartedDTO gameData) => NotifyAll(cb => cb.OnGameStarted(gameData));
        public void NotifyTurnChanged(TurnChangedDTO turnData) => NotifyAll(cb => cb.OnTurnChanged(turnData));
    }
}