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
        private const string LogPrefix = "[GAME CLIENT]";

        private const int MinimumTimeoutSeconds = 30;

        private const int ReconnectSemaphoreInitialCount = 1;
        private const int ReconnectSemaphoreMaxCount = 1;
        private const int NoWaitMilliseconds = 0;

        private int isReconnectionResultNotified;
        private int isReconnectionTickInProgress;

        private const int FlagFalse = 0;
        private const int FlagTrue = 1;

        private const string OperationConnectToGame = "ConnectToGame";
        private const string OperationLeaveGame = "LeaveGame";
        private const string OperationGameAction = "GameAction";
        private const string OperationReconnectToGame = "ReconnectToGame";

        private const int DefaultReconnectTimeoutSeconds = 30;

        private const int MaxReconnectionAttempts = 5;
        private const int ReconnectionIntervalMs = 5000;

        private readonly SynchronizationContext uiContext;
        private readonly SemaphoreSlim reconnectSemaphore;
        private readonly GameCallbackHandler callback;
        private readonly WcfConnectionGuardian guardian;

        private GameManagerClient client;
        private GameConnectionTimer connectionTimer;

        private string currentMatchCode;
        private int currentUserId;

        private int isConnectionLostNotified;

        private System.Timers.Timer reconnectionTimer;
        private bool isAttemptingReconnection;
        private int reconnectionAttempts;
        private bool userRequestedExit;

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

            reconnectSemaphore = new SemaphoreSlim(
                initialCount: ReconnectSemaphoreInitialCount,
                maxCount: ReconnectSemaphoreMaxCount);

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
                logger: new Logger());

            CreateProxy();
        }

        public void StartConnectionMonitoring(int timeoutSeconds)
        {
            int effectiveTimeoutSeconds =
                timeoutSeconds < MinimumTimeoutSeconds ? MinimumTimeoutSeconds : timeoutSeconds;

            Interlocked.Exchange(ref isConnectionLostNotified, FlagFalse);

            connectionTimer?.Dispose();
            connectionTimer = new GameConnectionTimer(effectiveTimeoutSeconds, OnConnectionTimeout);

            callback.SetConnectionTimer(connectionTimer);

            connectionTimer.Start();
            connectionTimer.NotifyActivity();

            Debug.WriteLine(string.Format("{0} [GAME TIMER] Started. TimeoutSeconds={1}", LogPrefix, effectiveTimeoutSeconds));
        }

        public void StopConnectionMonitoring()
        {
            try
            {
                connectionTimer?.Stop();
            }
            catch (ObjectDisposedException ex)
            {
                Debug.WriteLine(string.Format("{0} [GAME TIMER] Stop ObjectDisposedException: {1}", LogPrefix, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine(string.Format("{0} [GAME TIMER] Stop InvalidOperationException: {1}", LogPrefix, ex.Message));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("{0} [GAME TIMER] Stop UnexpectedException: {1}", LogPrefix, ex.Message));
            }
        }

        public async Task ConnectToGameAsync(string matchCode, int userId)
        {
            currentMatchCode = (matchCode ?? string.Empty).Trim();
            currentUserId = userId;

            MarkActivity();

            await guardian.ExecuteAsync(
                async () => await client.ConnectToGameAsync(currentMatchCode, currentUserId),
                operationName: OperationConnectToGame);

            MarkActivity();
        }

        public async Task LeaveGameAsync(string matchCode, int userId)
        {
            MarkActivity();

            await guardian.ExecuteAsync(
                async () => await client.LeaveGameAsync(matchCode, userId),
                operationName: OperationLeaveGame,
                suppressErrors: true);

            MarkActivity();
        }

        public Task<DrawCardResultCode> DrawCardAsync(string matchCode, int userId) =>
            ExecuteAsyncWithResult(
                () => client.DrawCardAsync(matchCode, userId),
                DrawCardResultCode.DrawCard_UnexpectedError);

        public Task<PlayCardResultCode> PlayDinoHeadAsync(string matchCode, int userId, int cardId) =>
            ExecuteAsyncWithResult(
                () => client.PlayDinoHeadAsync(matchCode, userId, cardId),
                PlayCardResultCode.PlayCard_UnexpectedError);

        public Task<PlayCardResultCode> AttachBodyPartAsync(string matchCode, int userId, AttachBodyPartDTO attachmentData) =>
            ExecuteAsyncWithResult(
                () => client.AttachBodyPartToDinoAsync(matchCode, userId, attachmentData),
                PlayCardResultCode.PlayCard_UnexpectedError);

        public Task<ProvokeResultCode> ProvokeArchArmyAsync(string matchCode, int userId, ArmyType armyType) =>
            ExecuteAsyncWithResult(
                () => client.ProvokeArchArmyAsync(matchCode, userId, armyType),
                ProvokeResultCode.Provoke_UnexpectedError);

        public Task<EndTurnResultCode> EndTurnAsync(string matchCode, int userId) =>
            ExecuteAsyncWithResult(
                () => client.EndTurnAsync(matchCode, userId),
                EndTurnResultCode.EndTurn_UnexpectedError);

        public Task<DrawCardResultCode> TakeCardFromDiscardPileAsync(string matchCode, int userId, int cardId) =>
            ExecuteAsyncWithResult(
                () => client.TakeCardFromDiscardPileAsync(matchCode, userId, cardId),
                DrawCardResultCode.DrawCard_UnexpectedError);

        private void OnConnectionTimeout()
        {
            _ = HandleTimeoutAsync();
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
                if (userRequestedExit || isAttemptingReconnection)
                {
                    return;
                }

                NotifyConnectionLostOnce();
            }
            finally
            {
                reconnectSemaphore.Release();
            }
        }


        public async Task<bool> TryReconnectToGameAsync()
        {
            try
            {
                if (!InternetConnectivity.HasInternet())
                {
                    return false;
                }

                StopConnectionMonitoring();

                EnsureClientIsUsable();

                bool connected = await guardian.ExecuteAsync(
                    async () => await client.ConnectToGameAsync(currentMatchCode, currentUserId),
                    operationName: OperationReconnectToGame,
                    suppressErrors: true);

                if (!connected)
                {
                    return false;
                }

                StartConnectionMonitoring(DefaultReconnectTimeoutSeconds);
                return true;
            }
            catch (EndpointNotFoundException ex)
            {
                Debug.WriteLine(string.Format("{0} TryReconnect EndpointNotFoundException: {1}", LogPrefix, ex.Message));
                return false;
            }
            catch (CommunicationException ex)
            {
                Debug.WriteLine(string.Format("{0} TryReconnect CommunicationException: {1}", LogPrefix, ex.Message));
                return false;
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine(string.Format("{0} TryReconnect TimeoutException: {1}", LogPrefix, ex.Message));
                return false;
            }
            catch (ObjectDisposedException ex)
            {
                Debug.WriteLine(string.Format("{0} TryReconnect ObjectDisposedException: {1}", LogPrefix, ex.Message));
                return false;
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine(string.Format("{0} TryReconnect InvalidOperationException: {1}", LogPrefix, ex.Message));
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("{0} TryReconnect UnexpectedException: {1}", LogPrefix, ex.Message));
                return false;
            }
        }

        private void StopReconnectionAttempts(bool success)
        {
            if (reconnectionTimer != null)
            {
                try
                {
                    reconnectionTimer.Stop();
                    reconnectionTimer.Elapsed -= OnReconnectionTimerElapsed;
                    reconnectionTimer.Dispose();
                }
                catch (ObjectDisposedException ex)
                {
                    Debug.WriteLine(string.Format("{0} StopReconnectionAttempts ObjectDisposedException: {1}", LogPrefix, ex.Message));
                }
                catch (InvalidOperationException ex)
                {
                    Debug.WriteLine(string.Format("{0} StopReconnectionAttempts InvalidOperationException: {1}", LogPrefix, ex.Message));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(string.Format("{0} StopReconnectionAttempts UnexpectedException: {1}", LogPrefix, ex.Message));
                }
                finally
                {
                    reconnectionTimer = null;
                }
            }

            isAttemptingReconnection = false;

            if (success)
            {
                Interlocked.Exchange(ref isConnectionLostNotified, FlagFalse);
            }
        }

        public void CancelReconnectionAndExit()
        {
            userRequestedExit = true;

            if (isAttemptingReconnection)
            {
                StopReconnectionAttempts(success: false);
            }
        }

        public void ForceAbort()
        {
            try
            {
                if (client is ICommunicationObject comm)
                {
                    comm.Abort();
                }
            }
            catch (ObjectDisposedException ex)
            {
                Debug.WriteLine(string.Format("{0} ForceAbort ObjectDisposedException: {1}", LogPrefix, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine(string.Format("{0} ForceAbort InvalidOperationException: {1}", LogPrefix, ex.Message));
            }
            catch (CommunicationException ex)
            {
                Debug.WriteLine(string.Format("{0} ForceAbort CommunicationException: {1}", LogPrefix, ex.Message));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("{0} ForceAbort UnexpectedException: {1}", LogPrefix, ex.Message));
            }
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
            try
            {
                var context = new InstanceContext(callback);
                context.SynchronizationContext = null;

                client = new GameManagerClient(context);

                HookClientLifecycleEvents(client);

                guardian.MonitorClientState(client);
            }
            catch (CommunicationException ex)
            {
                Debug.WriteLine(string.Format("{0} CreateProxy CommunicationException: {1}", LogPrefix, ex.Message));
                throw;
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine(string.Format("{0} CreateProxy TimeoutException: {1}", LogPrefix, ex.Message));
                throw;
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine(string.Format("{0} CreateProxy InvalidOperationException: {1}", LogPrefix, ex.Message));
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("{0} CreateProxy UnexpectedException: {1}", LogPrefix, ex.Message));
                throw;
            }
        }

        private void HookClientLifecycleEvents(GameManagerClient proxy)
        {
            if (proxy == null)
            {
                return;
            }

            if (proxy is ICommunicationObject comm)
            {
                comm.Faulted += OnClientFaultedOrClosed;
                comm.Closed += OnClientFaultedOrClosed;
            }
        }

        private void UnhookClientLifecycleEvents(GameManagerClient proxy)
        {
            if (proxy == null)
            {
                return;
            }

            if (proxy is ICommunicationObject comm)
            {
                comm.Faulted -= OnClientFaultedOrClosed;
                comm.Closed -= OnClientFaultedOrClosed;
            }
        }

        private void OnClientFaultedOrClosed(object sender, EventArgs e)
        {
            NotifyConnectionLostOnce();
        }

        public void StartReconnectionAttempts()
        {
            System.Threading.Interlocked.Exchange(ref isReconnectionTickInProgress, FlagFalse);
            System.Threading.Interlocked.Exchange(ref isReconnectionResultNotified, FlagFalse);

            if (string.IsNullOrWhiteSpace(currentMatchCode))
            {
                return;
            }

            if (isAttemptingReconnection)
            {
                return;
            }

            Debug.WriteLine(string.Format("{0} 🔄 Starting reconnection attempts...", LogPrefix));

            isAttemptingReconnection = true;
            reconnectionAttempts = 0;
            userRequestedExit = false;

            ReconnectionStarted?.Invoke();

            reconnectionTimer = new System.Timers.Timer(ReconnectionIntervalMs);
            reconnectionTimer.AutoReset = true;
            reconnectionTimer.Elapsed += OnReconnectionTimerElapsed;
            reconnectionTimer.Start();
        }

        private async void OnReconnectionTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (System.Threading.Interlocked.Exchange(ref isReconnectionTickInProgress, FlagTrue) == FlagTrue)
            {
                return;
            }

            try
            {
                ForceAbort();

                if (userRequestedExit)
                {
                    StopReconnectionAttempts(success: false);
                    NotifyReconnectionResult(false);
                    return;
                }

                reconnectionAttempts++;

                if (reconnectionAttempts > MaxReconnectionAttempts)
                {
                    StopReconnectionAttempts(success: false);
                    NotifyReconnectionResult(false);
                    return;
                }

                bool success = await TryReconnectToGameAsync();
                if (success)
                {
                    StopReconnectionAttempts(success: true);
                    NotifyReconnectionResult(true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("{0} OnReconnectionTimerElapsed UnexpectedException: {1}", LogPrefix, ex.Message));
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref isReconnectionTickInProgress, FlagFalse);
            }
        }

        private void NotifyReconnectionResult(bool success)
        {
            if (System.Threading.Interlocked.Exchange(ref isReconnectionResultNotified, FlagTrue) == FlagTrue)
            {
                return;
            }

            if (uiContext != null)
            {
                uiContext.Post(_ => ReconnectionCompleted?.Invoke(success), null);
                return;
            }

            Application.Current?.Dispatcher?.Invoke(() => ReconnectionCompleted?.Invoke(success));
        }


        private void CloseProxy()
        {
            UnhookClientLifecycleEvents(client);

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
                    Debug.WriteLine(string.Format("{0} CloseProxy CommunicationException: {1}", LogPrefix, ex.Message));
                    TryAbort(comm);
                }
                catch (TimeoutException ex)
                {
                    Debug.WriteLine(string.Format("{0} CloseProxy TimeoutException: {1}", LogPrefix, ex.Message));
                    TryAbort(comm);
                }
                catch (ObjectDisposedException ex)
                {
                    Debug.WriteLine(string.Format("{0} CloseProxy ObjectDisposedException: {1}", LogPrefix, ex.Message));
                    TryAbort(comm);
                }
                catch (InvalidOperationException ex)
                {
                    Debug.WriteLine(string.Format("{0} CloseProxy InvalidOperationException: {1}", LogPrefix, ex.Message));
                    TryAbort(comm);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(string.Format("{0} CloseProxy UnexpectedException: {1}", LogPrefix, ex.Message));
                    TryAbort(comm);
                }
            }
        }

        private void TryAbort(ICommunicationObject comm)
        {
            if (comm == null)
            {
                return;
            }

            try
            {
                comm.Abort();
            }
            catch (ObjectDisposedException ex)
            {
                Debug.WriteLine(string.Format("{0} TryAbort ObjectDisposedException: {1}", LogPrefix, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine(string.Format("{0} TryAbort InvalidOperationException: {1}", LogPrefix, ex.Message));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("{0} TryAbort UnexpectedException: {1}", LogPrefix, ex.Message));
            }
        }

        private void MarkActivity()
        {
            Interlocked.Exchange(ref isConnectionLostNotified, FlagFalse);
            connectionTimer?.NotifyActivity();
        }

        private async Task<T> ExecuteAsyncWithResult<T>(Func<Task<T>> action, T defaultErrorValue)
        {
            MarkActivity();

            T result = await guardian.ExecuteAsync(
                async () => await action(),
                defaultValue: defaultErrorValue,
                operationName: OperationGameAction);

            MarkActivity();
            return result;
        }

        private void NotifyConnectionLostOnce()
        {
            int previous = Interlocked.Exchange(ref isConnectionLostNotified, FlagTrue);
            if (previous == FlagTrue)
            {
                return;
            }

            StopConnectionMonitoring();
            RaiseConnectionLost();
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

        public async Task DisconnectAsync()
        {
            try
            {
                CancelReconnectionAndExit();
                StopConnectionMonitoring();

                if (!string.IsNullOrWhiteSpace(currentMatchCode))
                {
                    await LeaveGameAsync(currentMatchCode, currentUserId);
                }
            }
            catch (EndpointNotFoundException ex)
            {
                Debug.WriteLine(string.Format("{0} DisconnectAsync EndpointNotFoundException: {1}", LogPrefix, ex.Message));
            }
            catch (CommunicationException ex)
            {
                Debug.WriteLine(string.Format("{0} DisconnectAsync CommunicationException: {1}", LogPrefix, ex.Message));
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine(string.Format("{0} DisconnectAsync TimeoutException: {1}", LogPrefix, ex.Message));
            }
            catch (ObjectDisposedException ex)
            {
                Debug.WriteLine(string.Format("{0} DisconnectAsync ObjectDisposedException: {1}", LogPrefix, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine(string.Format("{0} DisconnectAsync InvalidOperationException: {1}", LogPrefix, ex.Message));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("{0} DisconnectAsync UnexpectedException: {1}", LogPrefix, ex.Message));
            }
            finally
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            CancelReconnectionAndExit();

            try
            {
                connectionTimer?.Dispose();
                connectionTimer = null;
            }
            catch (ObjectDisposedException ex)
            {
                Debug.WriteLine(string.Format("{0} Dispose timer ObjectDisposedException: {1}", LogPrefix, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine(string.Format("{0} Dispose timer InvalidOperationException: {1}", LogPrefix, ex.Message));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("{0} Dispose timer UnexpectedException: {1}", LogPrefix, ex.Message));
            }

            try
            {
                CloseProxy();
            }
            catch (CommunicationException ex)
            {
                Debug.WriteLine(string.Format("{0} Dispose CloseProxy CommunicationException: {1}", LogPrefix, ex.Message));
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine(string.Format("{0} Dispose CloseProxy TimeoutException: {1}", LogPrefix, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine(string.Format("{0} Dispose CloseProxy InvalidOperationException: {1}", LogPrefix, ex.Message));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("{0} Dispose CloseProxy UnexpectedException: {1}", LogPrefix, ex.Message));
            }
            finally
            {
                client = null;
            }
        }

        public Task InitializeGameAsync(string matchCode) => Task.CompletedTask;
        public Task StartGameAsync(string matchCode) => Task.CompletedTask;
    }
}