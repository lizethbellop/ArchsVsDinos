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
        private System.Timers.Timer reconnectionTimer;
        private bool isAttemptingReconnection = false;
        private int reconnectionAttempts = 0;
        private const int MaxReconnectionAttempts = 5;
        private const int ReconnectionIintervalMs = 5000;
        private bool userRequestedExit = false;

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
        public event Action ReconnectionStarted;
        public event Action<bool> ReconnectionCompleted;

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
            int effectiveTimeoutSeconds = timeoutSeconds < MinimumTimeoutSeconds ? MinimumTimeoutSeconds : timeoutSeconds;
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
            await guardian.ExecuteAsync(async () => await client.ConnectToGameAsync(currentMatchCode, currentUserId), OperationConnectToGame);
            MarkActivity();
        }

        public async Task LeaveGameAsync(string matchCode, int userId)
        {
            MarkActivity();
            await guardian.ExecuteAsync(async () => await client.LeaveGameAsync(matchCode, userId), OperationLeaveGame, suppressErrors: true);
            MarkActivity();
        }

        // Métodos de acción del juego
        public Task<DrawCardResultCode> DrawCardAsync(string matchCode, int userId) =>
            ExecuteAsyncWithResult(() => client.DrawCardAsync(matchCode, userId), DrawCardResultCode.DrawCard_UnexpectedError);

        public Task<PlayCardResultCode> PlayDinoHeadAsync(string matchCode, int userId, int cardId) =>
            ExecuteAsyncWithResult(() => client.PlayDinoHeadAsync(matchCode, userId, cardId), PlayCardResultCode.PlayCard_UnexpectedError);

        public Task<PlayCardResultCode> AttachBodyPartAsync(string matchCode, int userId, AttachBodyPartDTO attachmentData) =>
            ExecuteAsyncWithResult(() => client.AttachBodyPartToDinoAsync(matchCode, userId, attachmentData), PlayCardResultCode.PlayCard_UnexpectedError);

        public Task<ProvokeResultCode> ProvokeArchArmyAsync(string matchCode, int userId, ArmyType armyType) =>
            ExecuteAsyncWithResult(() => client.ProvokeArchArmyAsync(matchCode, userId, armyType), ProvokeResultCode.Provoke_UnexpectedError);

        public Task<EndTurnResultCode> EndTurnAsync(string matchCode, int userId) =>
            ExecuteAsyncWithResult(() => client.EndTurnAsync(matchCode, userId), EndTurnResultCode.EndTurn_UnexpectedError);

        public Task<DrawCardResultCode> TakeCardFromDiscardPileAsync(string matchCode, int userId, int cardId) =>
            ExecuteAsyncWithResult(() => client.TakeCardFromDiscardPileAsync(matchCode, userId, cardId), DrawCardResultCode.DrawCard_UnexpectedError);

        private void OnConnectionTimeout()
        {
            _ = HandleTimeoutAsync();
        }

        public void StartReconnectionAttempts()
        {
            if (string.IsNullOrEmpty(currentMatchCode) || isAttemptingReconnection) return;

            Debug.WriteLine("[GAME CLIENT] 🔄 Iniciando reconexión automática...");
            isAttemptingReconnection = true;
            reconnectionAttempts = 0;
            userRequestedExit = false;

            ReconnectionStarted?.Invoke();

            reconnectionTimer = new System.Timers.Timer(ReconnectionIintervalMs);
            reconnectionTimer.Elapsed += OnReconnectionTimerElapsed;
            reconnectionTimer.AutoReset = true;
            reconnectionTimer.Start();
        }

        private async void OnReconnectionTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            ForceAbort();

            if (userRequestedExit)
            {
                StopReconnectionAttempts(success: false);
                return;
            }

            reconnectionAttempts++;
            if (reconnectionAttempts > MaxReconnectionAttempts)
            {
                StopReconnectionAttempts(success: false);
                NotifyReconnectionResult(false);
                return;
            }

            if (await TryReconnectToGameAsync())
            {
                StopReconnectionAttempts(success: true);
                NotifyReconnectionResult(true);
            }
        }

        public async Task<bool> TryReconnectToGameAsync()
        {
            try
            {
                if (!InternetConnectivity.HasInternet()) return false;
                connectionTimer?.Stop();
                EnsureClientIsUsable();

                bool connected = await guardian.ExecuteAsync(async () =>
                {
                    await client.ConnectToGameAsync(currentMatchCode, currentUserId);
                }, operationName: "ReconnectToGame", suppressErrors: true);

                if (connected)
                {
                    connectionTimer?.Start();
                    connectionTimer?.NotifyActivity();
                    consecutiveTimeoutCount = 0;
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        private void StopReconnectionAttempts(bool success)
        {
            if (reconnectionTimer != null)
            {
                reconnectionTimer.Stop();
                reconnectionTimer.Elapsed -= OnReconnectionTimerElapsed;
                reconnectionTimer.Dispose();
                reconnectionTimer = null;
            }
            isAttemptingReconnection = false;
            if (success) StartConnectionMonitoring(30);
        }

        public void CancelReconnectionAndExit()
        {
            userRequestedExit = true;
            if (isAttemptingReconnection) StopReconnectionAttempts(false);
        }

        public void ForceAbort()
        {
            try { ((ICommunicationObject)client)?.Abort(); } catch { }
        }

        // --- EL ORDEN QUE SOLICITASTE COMIENZA AQUÍ ---

        private async Task HandleTimeoutAsync()
        {
            bool entered = await reconnectSemaphore.WaitAsync(NoWaitMilliseconds);
            if (!entered) return;

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
            if (client == null) return CommunicationState.Closed;
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

            client.Endpoint.Binding.SendTimeout = TimeSpan.FromSeconds(10);
            client.Endpoint.Binding.OpenTimeout = TimeSpan.FromSeconds(10);
            client.Endpoint.Binding.ReceiveTimeout = TimeSpan.FromSeconds(20);

            guardian.MonitorClientState(client);
        }

        private void CloseProxy()
        {
            if (client is ICommunicationObject comm)
            {
                try
                {
                    if (comm.State == CommunicationState.Faulted) comm.Abort();
                    else comm.Close();
                }
                catch { comm.Abort(); }
            }
        }

        private void MarkActivity()
        {
            connectionTimer?.NotifyActivity();
        }

        // --- Helpers y Notificaciones finales ---

        private async Task<T> ExecuteAsyncWithResult<T>(Func<Task<T>> action, T defaultErrorValue)
        {
            MarkActivity();
            T result = await guardian.ExecuteAsync(async () => await action(), defaultValue: defaultErrorValue, operationName: OperationGameAction);
            MarkActivity();
            return result;
        }

        private void NotifyReconnectionResult(bool success)
        {
            if (uiContext != null) uiContext.Post(_ => ReconnectionCompleted?.Invoke(success), null);
            else Application.Current?.Dispatcher?.Invoke(() => ReconnectionCompleted?.Invoke(success));
        }

        private void RaiseServiceError(string title, string message)
        {
            if (uiContext != null) uiContext.Post(_ => ServiceError?.Invoke(title, message), null);
            else Application.Current?.Dispatcher?.Invoke(() => ServiceError?.Invoke(title, message));
        }

        private void RaiseConnectionLost()
        {
            if (uiContext != null) uiContext.Post(_ => ConnectionLost?.Invoke(), null);
            else Application.Current?.Dispatcher?.Invoke(() => ConnectionLost?.Invoke());
        }

        public async Task DisconnectAsync()
        {
            try
            {
                StopConnectionMonitoring();
                if (!string.IsNullOrWhiteSpace(currentMatchCode)) await LeaveGameAsync(currentMatchCode, currentUserId);
            }
            catch { }
            finally { Dispose(); }
        }

        public void Dispose()
        {
            connectionTimer?.Dispose();
            connectionTimer = null;
            CloseProxy();
            client = null;
        }

        public Task InitializeGameAsync(string matchCode) => Task.CompletedTask;
        public Task StartGameAsync(string matchCode) => Task.CompletedTask;
    }
}