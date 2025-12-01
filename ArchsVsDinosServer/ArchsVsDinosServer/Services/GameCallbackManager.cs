using ArchsVsDinosServer.Interfaces;
using Contracts;
using Contracts.DTO;
using Contracts.DTO.Game_DTO;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace ArchsVsDinosServer.Services
{
    public class GameCallbackManager
    {
        private readonly List<IGameManagerCallback> activeCallbacks = new List<IGameManagerCallback>();
        private readonly object syncRoot = new object();
        private readonly ILoggerHelper loggerHelper;

        public GameCallbackManager(ILoggerHelper loggerHelper)
        {
            this.loggerHelper = loggerHelper ?? throw new ArgumentNullException(nameof(loggerHelper), "Logger helper cannot be null.");
        }

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
                        loggerHelper.LogInfo("New player callback registered.");
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError("Failed to register callback due to invalid operation.", ex);
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError("Communication error while registering callback.", ex);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError("Unexpected error while registering callback.", ex);
            }
        }

        public void UnregisterCallback(IGameManagerCallback callback)
        {
            try
            {
                if (callback != null)
                {
                    lock (syncRoot)
                    {
                        activeCallbacks.Remove(callback);
                        loggerHelper.LogInfo("Player callback unregistered.");
                    }
                }
                else
                {
                    loggerHelper.LogWarning("Attempted to unregister a null callback.");
                }
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError("Failed to unregister callback due to invalid operation.", ex);
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError("Communication error while unregistering callback.", ex);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError("Unexpected error while unregistering callback.", ex);
            }
        }

        private void NotifyAll(Action<IGameManagerCallback> action)
        {
            List<IGameManagerCallback> failedCallbacks = new List<IGameManagerCallback>();

            lock (syncRoot)
            {
                foreach (var callback in activeCallbacks)
                {
                    try
                    {
                        action(callback);
                    }
                    catch (CommunicationException ex)
                    {
                        failedCallbacks.Add(callback);
                        loggerHelper.LogError("Communication error while notifying a player.", ex);
                    }
                    catch (InvalidOperationException ex)
                    {
                        failedCallbacks.Add(callback);
                        loggerHelper.LogError("Invalid operation while notifying a player.", ex);
                    }
                    catch (Exception ex)
                    {
                        failedCallbacks.Add(callback);
                        loggerHelper.LogError("Unexpected error while notifying a player.", ex);
                    }
                }

                foreach (var failed in failedCallbacks)
                {
                    activeCallbacks.Remove(failed);
                    loggerHelper.LogWarning("Removed a failed player callback from active list.");
                }
            }
        }

        public void NotifyGameInitialized(GameInitializedDTO gameData)
        {
            try
            {
                NotifyAll(callback => callback.OnGameInitialized(gameData));
                loggerHelper.LogInfo("Notified all players: Game initialized.");
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError("Invalid operation in NotifyGameInitialized.", ex);
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError("Communication error in NotifyGameInitialized.", ex);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError("Unexpected error in NotifyGameInitialized.", ex);
            }
        }

        public void NotifyGameStarted(GameStartedDTO gameData)
        {
            try
            {
                NotifyAll(callback => callback.OnGameStarted(gameData));
                loggerHelper.LogInfo("Notified all players: Game started.");
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError("Invalid operation in NotifyGameStarted.", ex);
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError("Communication error in NotifyGameStarted.", ex);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError("Unexpected error in NotifyGameStarted.", ex);
            }
        }

        public void NotifyTurnChanged(TurnChangedDTO turnData)
        {
            try
            {
                NotifyAll(callback => callback.OnTurnChanged(turnData));
                loggerHelper.LogInfo($"Notified all players: Turn changed to next player.");
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError("Invalid operation in NotifyTurnChanged.", ex);
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError("Communication error in NotifyTurnChanged.", ex);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError("Unexpected error in NotifyTurnChanged.", ex);
            }
        }

        public void NotifyCardDrawn(CardDrawnDTO cardData)
        {
            try
            {
                NotifyAll(callback => callback.OnCardDrawn(cardData));
                loggerHelper.LogInfo($"Notified all players: Player drew a card.");
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError("Invalid operation in NotifyCardDrawn.", ex);
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError("Communication error in NotifyCardDrawn.", ex);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError("Unexpected error in NotifyCardDrawn.", ex);
            }
        }

        public void NotifyDinoHeadPlayed(DinoPlayedDTO dinoData)
        {
            try
            {
                NotifyAll(callback => callback.OnDinoHeadPlayed(dinoData));
                loggerHelper.LogInfo($"Notified all players: Player played a dinosaur head.");
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError("Invalid operation in NotifyDinoHeadPlayed.", ex);
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError("Communication error in NotifyDinoHeadPlayed.", ex);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError("Unexpected error in NotifyDinoHeadPlayed.", ex);
            }
        }

        public void NotifyBodyPartAttached(BodyPartAttachedDTO bodyPartData)
        {
            try
            {
                NotifyAll(callback => callback.OnBodyPartAttached(bodyPartData));
                loggerHelper.LogInfo($"Notified all players: Player attached a body part.");
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError("Invalid operation in NotifyBodyPartAttached.", ex);
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError("Communication error in NotifyBodyPartAttached.", ex);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError("Unexpected error in NotifyBodyPartAttached.", ex);
            }
        }

        public void NotifyArchAddedToBoard(ArchAddedToBoardDTO archData)
        {
            try
            {
                NotifyAll(callback => callback.OnArchAddedToBoard(archData));
                loggerHelper.LogInfo($"Notified all players: Arch added to board.");
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError("Invalid operation in NotifyArchAddedToBoard.", ex);
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError("Communication error in NotifyArchAddedToBoard.", ex);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError("Unexpected error in NotifyArchAddedToBoard.", ex);
            }
        }

        public void NotifyArchArmyProvoked(ArchArmyProvokedDTO provokeData)
        {
            try
            {
                NotifyAll(callback => callback.OnArchArmyProvoked(provokeData));
                loggerHelper.LogInfo($"Notified all players: Arch army provoked.");
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError("Invalid operation in NotifyArchArmyProvoked.", ex);
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError("Communication error in NotifyArchArmyProvoked.", ex);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError("Unexpected error in NotifyArchArmyProvoked.", ex);
            }
        }

        public void NotifyBattleResolved(BattleResultDTO battleData)
        {
            try
            {
                NotifyAll(callback => callback.OnBattleResolved(battleData));
                loggerHelper.LogInfo("Notified all players: Battle resolved.");
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError("Invalid operation in NotifyBattleResolved.", ex);
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError("Communication error in NotifyBattleResolved.", ex);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError("Unexpected error in NotifyBattleResolved.", ex);
            }
        }

        public void NotifyGameEnded(GameEndedDTO endData)
        {
            try
            {
                NotifyAll(callback => callback.OnGameEnded(endData));
                loggerHelper.LogInfo("Notified all players: Game ended.");
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError("Invalid operation in NotifyGameEnded.", ex);
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError("Communication error in NotifyGameEnded.", ex);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError("Unexpected error in NotifyGameEnded.", ex);
            }
        }

        public void NotifyPlayerExpelled(PlayerExpelledDTO playerData)
        {
            try
            {
                NotifyAll(callback => callback.OnPlayerExpelled(playerData));
                loggerHelper.LogInfo($"Notified all players: Player expelled.");
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError("Invalid operation in NotifyPlayerExpelled.", ex);
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError("Communication error in NotifyPlayerExpelled.", ex);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError("Unexpected error in NotifyPlayerExpelled.", ex);
            }
        }
    }
}
