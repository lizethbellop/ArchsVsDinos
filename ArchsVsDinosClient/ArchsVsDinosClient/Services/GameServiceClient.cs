using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.GameService;
using ArchsVsDinosClient.Logging;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient.Services
{
    public sealed class GameServiceClient : IGameServiceClient, IDisposable
    {
        private const int MinimumTimeoutSeconds = 30;
        private const int MaxConsecutiveTimeouts = 3;

        private const int ReconnectSemaphoreInitialCount = 1;
        private const int ReconnectSemaphoreMaxCount = 1;

        private const int NoWaitMilliseconds = 0;

        private const string OperationConnectToGame = "ConnectToGame";
        private const string OperationLeaveGame = "LeaveGame";
        private const string OperationGameAction = "GameAction";

        private readonly SynchronizationContext uiContext;
        private readonly SemaphoreSlim reconnectSemaphore;

        private readonly GameCallbackHandler callback;
        private readonly WcfConnectionGuardian guardian;

        private GameManagerClient client;
        private GameConnectionTimer connectionTimer;

        private int consecutiveTimeoutCount;
        private string currentMatchCode;
        private int currentUserId;

        public event Action<GameInitializedDTO> GameInitialized;
        public event Action<GameStartedDTO> GameStarted;
        public event Action<TurnChangedDTO> TurnChanged;
        public event Action<CardDrawnDTO> CardDrawn;
        public event Action<DinoPlayedDTO> DinoHeadPlayed;
        public event Action<BodyPartAttachedDTO> BodyPartAttached;
        public event Action<ArchAddedToBoardDTO> ArchAdded;
        public event Action<ArchArmyProvokedDTO> ArchProvoked;
        public event Action<BattleResultDTO> BattleResolved;
        public event Action<GameEndedDTO> GameEnded;
        public event Action<PlayerExpelledDTO> PlayerExpelled;
        public event Action<CardTakenFromDiscardDTO> CardTakenFromDiscard;
        public event Action<string, string> ServiceError;
        public event Action ConnectionLost;

        public GameServiceClient()
        {
            uiContext = SynchronizationContext.Current;
            reconnectSemaphore = new SemaphoreSlim(ReconnectSemaphoreInitialCount, ReconnectSemaphoreMaxCount);

            consecutiveTimeoutCount = 0;
            currentMatchCode = string.Empty;
            currentUserId = 0;

            callback = new GameCallbackHandler();

            callback.OnGameInitializedEvent += dto => GameInitialized?.Invoke(dto);
            callback.OnGameStartedEvent += dto => GameStarted?.Invoke(dto);
            callback.OnTurnChangedEvent += dto => TurnChanged?.Invoke(dto);
            callback.OnCardDrawnEvent += dto => CardDrawn?.Invoke(dto);
            callback.OnDinoPlayedEvent += dto => DinoHeadPlayed?.Invoke(dto);
            callback.OnBodyPartAttachedEvent += dto => BodyPartAttached?.Invoke(dto);
            callback.OnArchAddedEvent += dto => ArchAdded?.Invoke(dto);
            callback.OnArchProvokedEvent += dto => ArchProvoked?.Invoke(dto);
            callback.OnBattleResolvedEvent += dto => BattleResolved?.Invoke(dto);
            callback.OnGameEndedEvent += dto => GameEnded?.Invoke(dto);
            callback.OnPlayerExpelledEvent += dto => PlayerExpelled?.Invoke(dto);
            callback.OnCardTakenFromDiscardEvent += dto => CardTakenFromDiscard?.Invoke(dto);

            guardian = new WcfConnectionGuardian(
                onError: (title, message) => RaiseServiceError(title, message),
                logger: new Logger()
            );

            CreateProxy();
        }

        public void StartConnectionMonitoring(int timeoutSeconds)
        {
            int effectiveTimeoutSeconds = timeoutSeconds < MinimumTimeoutSeconds
                ? MinimumTimeoutSeconds
                : timeoutSeconds;

            consecutiveTimeoutCount = 0;

            connectionTimer?.Dispose();
            connectionTimer = new GameConnectionTimer(effectiveTimeoutSeconds, OnConnectionTimeout);

            callback.SetConnectionTimer(connectionTimer);

            connectionTimer.Start();
            connectionTimer.NotifyActivity();

            Debug.WriteLine($"[GAME TIMER] Started. TimeoutSeconds={effectiveTimeoutSeconds}");
        }

        public void StopConnectionMonitoring()
        {
            connectionTimer?.Stop();
            Debug.WriteLine("[GAME TIMER] Stopped");
        }

        public async Task ConnectToGameAsync(string matchCode, int userId)
        {
            currentMatchCode = matchCode ?? string.Empty;
            currentUserId = userId;

            MarkActivity();

            await guardian.ExecuteAsync(
                operation: async () => await client.ConnectToGameAsync(currentMatchCode, currentUserId),
                operationName: OperationConnectToGame
            );

            MarkActivity();
        }

        public async Task LeaveGameAsync(string matchCode, int userId)
        {
            MarkActivity();

            await guardian.ExecuteAsync(
                operation: async () => await client.LeaveGameAsync(matchCode, userId),
                operationName: OperationLeaveGame,
                suppressErrors: true
            );

            MarkActivity();
        }

        public Task InitializeGameAsync(string matchCode)
        {
            return Task.CompletedTask;
        }

        public Task StartGameAsync(string matchCode)
        {
            return Task.CompletedTask;
        }

        public Task<DrawCardResultCode> DrawCardAsync(string matchCode, int userId)
        {
            return ExecuteAsyncWithResult(
                action: () => client.DrawCardAsync(matchCode, userId),
                defaultErrorValue: DrawCardResultCode.DrawCard_UnexpectedError
            );
        }

        public Task<PlayCardResultCode> PlayDinoHeadAsync(string matchCode, int userId, int cardId)
        {
            return ExecuteAsyncWithResult(
                action: () => client.PlayDinoHeadAsync(matchCode, userId, cardId),
                defaultErrorValue: PlayCardResultCode.PlayCard_UnexpectedError
            );
        }

        public Task<PlayCardResultCode> AttachBodyPartAsync(string matchCode, int userId, AttachBodyPartDTO attachmentData)
        {
            return ExecuteAsyncWithResult(
                action: () => client.AttachBodyPartToDinoAsync(matchCode, userId, attachmentData),
                defaultErrorValue: PlayCardResultCode.PlayCard_UnexpectedError
            );
        }

        public Task<ProvokeResultCode> ProvokeArchArmyAsync(string matchCode, int userId, ArmyType armyType)
        {
            return ExecuteAsyncWithResult(
                action: () => client.ProvokeArchArmyAsync(matchCode, userId, armyType),
                defaultErrorValue: ProvokeResultCode.Provoke_UnexpectedError
            );
        }

        public Task<EndTurnResultCode> EndTurnAsync(string matchCode, int userId)
        {
            return ExecuteAsyncWithResult(
                action: () => client.EndTurnAsync(matchCode, userId),
                defaultErrorValue: EndTurnResultCode.EndTurn_UnexpectedError
            );
        }

        public Task<DrawCardResultCode> TakeCardFromDiscardPileAsync(string matchCode, int userId, int cardId)
        {
            return ExecuteAsyncWithResult(
                action: () => client.TakeCardFromDiscardPileAsync(matchCode, userId, cardId),
                defaultErrorValue: DrawCardResultCode.DrawCard_UnexpectedError
            );
        }

        private void OnConnectionTimeout()
        {
            Task ignoredTask = HandleTimeoutAsync();
        }

        private async Task HandleTimeoutAsync()
        {
            bool entered = await reconnectSemaphore.WaitAsync(NoWaitMilliseconds);
            if (!entered)
            {
                return;
            }

            try
            {
                CommunicationState state = GetClientState();

                if (state == CommunicationState.Opened)
                {
                    consecutiveTimeoutCount = 0;
                    RestartMonitoringAfterInactivity();
                    return;
                }

                consecutiveTimeoutCount++;

                Debug.WriteLine($"[GAME TIMER] Timeout with channel state={state}. Count={consecutiveTimeoutCount}");

                EnsureClientIsUsable();

                if (consecutiveTimeoutCount >= MaxConsecutiveTimeouts)
                {
                    RaiseConnectionLost();
                    return;
                }

                RestartMonitoringAfterInactivity();
            }
            finally
            {
                reconnectSemaphore.Release();
            }
        }

        private void RestartMonitoringAfterInactivity()
        {
            connectionTimer?.Start();
            connectionTimer?.NotifyActivity();
            Debug.WriteLine("[GAME TIMER] Inactivity handled (monitor restarted).");
        }

        private void EnsureClientIsUsable()
        {
            if (client == null)
            {
                CreateProxy();
                return;
            }

            CommunicationState state = ((ICommunicationObject)client).State;
            if (state == CommunicationState.Faulted || state == CommunicationState.Closed)
            {
                ResetProxy();
            }
        }

        private CommunicationState GetClientState()
        {
            if (client == null)
            {
                return CommunicationState.Closed;
            }

            return ((ICommunicationObject)client).State;
        }

        private void ResetProxy()
        {
            CloseProxy();
            CreateProxy();

            if (connectionTimer != null)
            {
                callback.SetConnectionTimer(connectionTimer);
            }
        }

        private void CreateProxy()
        {
            var context = new InstanceContext(callback);
            context.SynchronizationContext = null;

            client = new GameManagerClient(context);
            guardian.MonitorClientState(client);
        }

        private void CloseProxy()
        {
            if (client is ICommunicationObject comm)
            {
                try
                {
                    if (comm.State == CommunicationState.Faulted)
                    {
                        comm.Abort();
                    }
                    else
                    {
                        comm.Close();
                    }
                }
                catch (CommunicationException ex)
                {
                    Debug.WriteLine($"[GAME CLIENT] CloseProxy CommunicationException: {ex.Message}");
                    comm.Abort();
                }
                catch (TimeoutException ex)
                {
                    Debug.WriteLine($"[GAME CLIENT] CloseProxy TimeoutException: {ex.Message}");
                    comm.Abort();
                }
                catch (InvalidOperationException ex)
                {
                    Debug.WriteLine($"[GAME CLIENT] CloseProxy InvalidOperationException: {ex.Message}");
                    comm.Abort();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[GAME CLIENT] CloseProxy Exception: {ex.Message}");
                }
            }
        }

        private void MarkActivity()
        {
            connectionTimer?.NotifyActivity();
        }

        private async Task<T> ExecuteAsyncWithResult<T>(Func<Task<T>> action, T defaultErrorValue)
        {
            MarkActivity();

            T result = await guardian.ExecuteAsync(
                operation: async () => await action(),
                defaultValue: defaultErrorValue,
                operationName: OperationGameAction
            );

            MarkActivity();
            return result;
        }

        private void RaiseServiceError(string title, string message)
        {
            if (uiContext != null)
            {
                uiContext.Post(_ => ServiceError?.Invoke(title, message), null);
                return;
            }

            Application.Current?.Dispatcher?.Invoke(() => ServiceError?.Invoke(title, message));
        }

        private void RaiseConnectionLost()
        {
            if (uiContext != null)
            {
                uiContext.Post(_ => ConnectionLost?.Invoke(), null);
                return;
            }

            Application.Current?.Dispatcher?.Invoke(() => ConnectionLost?.Invoke());
        }

        public void Dispose()
        {
            connectionTimer?.Dispose();
            connectionTimer = null;

            CloseProxy();
            client = null;
        }

        public async Task DisconnectAsync()
        {
            try
            {
                StopConnectionMonitoring();

                string matchCode = currentMatchCode ?? string.Empty;
                int userId = currentUserId;

                if (!string.IsNullOrWhiteSpace(matchCode))
                {
                    await LeaveGameAsync(matchCode, userId);
                }
            }
            catch (CommunicationException ex)
            {
                Debug.WriteLine($"[GAME CLIENT] DisconnectAsync CommunicationException: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine($"[GAME CLIENT] DisconnectAsync TimeoutException: {ex.Message}");
            }

            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"[GAME CLIENT] DisconnectAsync InvalidOperationException: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GAME CLIENT] DisconnectAsync Exception: {ex.Message}");
            }
            finally
            {
                Dispose();
            }
        }

    }
}