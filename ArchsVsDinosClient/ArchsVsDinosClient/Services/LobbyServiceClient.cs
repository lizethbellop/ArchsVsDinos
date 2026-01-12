using ArchsVsDinosClient.LobbyService;
using ArchsVsDinosClient.Logging;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient.Services
{
    public class LobbyServiceClient : ILobbyServiceClient
    {
        private const int DefaultMaxPlayers = 4;
        private const int MinimumTimeoutSeconds = 30;
        private const int MaxConsecutiveTimeouts = 3;
        private readonly SynchronizationContext uiContext;
        private readonly SemaphoreSlim reconnectSemaphore;

        private const int HeartbeatIntervalMs = 4000;
        private const int MaxHeartbeatFailures = 2; 
        private System.Timers.Timer heartbeatTimer;
        private int heartbeatFailures = 0;

        private int consecutiveTimeoutCount;

        private LobbyManagerClient lobbyManagerClient;
        private readonly LobbyCallbackManager lobbyCallbackManager;
        private readonly WcfConnectionGuardian connectionGuardian;
        private GameConnectionTimer connectionTimer;

        public event Action<ArchsVsDinosClient.DTO.LobbyPlayerDTO, string> LobbyCreated;
        public event Action<ArchsVsDinosClient.DTO.LobbyPlayerDTO> PlayerJoined;
        public event Action<ArchsVsDinosClient.DTO.LobbyPlayerDTO> PlayerLeft;
        public event Action<string> GameStartedEvent;
        public event Action<string, bool> PlayerReadyEvent;
        public event Action<string, string> ConnectionError;
        public event Action<List<ArchsVsDinosClient.DTO.LobbyPlayerDTO>> PlayerListUpdated;
        public event Action<string, string> PlayerKickedEvent;
        public event Action<LobbyInvitationDTO> LobbyInvitationReceived;
        public event Action ConnectionLost;

        public LobbyServiceClient()
        {
            uiContext = SynchronizationContext.Current;
            reconnectSemaphore = new SemaphoreSlim(1, 1);
            consecutiveTimeoutCount = 0;

            lobbyCallbackManager = new LobbyCallbackManager();

            lobbyCallbackManager.OnJoinedLobby += playerDto => PlayerJoined?.Invoke(playerDto);
            lobbyCallbackManager.OnPlayerLeftLobby += playerDto => PlayerLeft?.Invoke(playerDto);
            lobbyCallbackManager.OnPlayerListUpdated += playerList => PlayerListUpdated?.Invoke(playerList);
            lobbyCallbackManager.OnPlayerReady += (nickname, isReady) => PlayerReadyEvent?.Invoke(nickname, isReady);
            lobbyCallbackManager.OnGameStart += () => GameStartedEvent?.Invoke(string.Empty);
            lobbyCallbackManager.OnLobbyInvitationReceived += invitation => LobbyInvitationReceived?.Invoke(invitation);

            lobbyCallbackManager.OnPlayerKicked += (nickname, reason) => PlayerKickedEvent?.Invoke(nickname, reason);

            var instanceContext = new InstanceContext(lobbyCallbackManager);
            if (uiContext != null)
            {
                instanceContext.SynchronizationContext = uiContext;
            }

            lobbyManagerClient = new LobbyManagerClient(instanceContext);

            connectionGuardian = new WcfConnectionGuardian(
                onError: (title, message) => RaiseConnectionError(title, message),
                logger: new Logger()
            );

            connectionGuardian.MonitorClientState(lobbyManagerClient);
        }


        public async Task<MatchCreationResultCode> CreateLobbyAsync(ArchsVsDinosClient.DTO.UserAccountDTO userAccount)
        {
            var matchSettings = new ArchsVsDinosClient.LobbyService.MatchSettings
            {
                HostNickname = UserSession.Instance.CurrentUser.Nickname,
                HostUsername = UserSession.Instance.CurrentUser.Username,
                MaxPlayers = DefaultMaxPlayers,
                HostUserId = UserSession.Instance.CurrentUser.IdUser
            };

            try
            {
                EnsureClientIsUsable();

                var response = await connectionGuardian.ExecuteWithThrowAsync(() =>
                    Task.FromResult(lobbyManagerClient.CreateLobby(matchSettings)));

                if (response.Success)
                {
                    UserSession.Instance.CurrentMatchCode = response.LobbyCode;
                    await ConnectToLobbyAsync(response.LobbyCode, matchSettings.HostNickname);
                }

                return response.ResultCode;
            }
            catch (EndpointNotFoundException ex)
            {
                ResetLobbyClient();
                RaiseConnectionError(Lang.GlobalSystemError, Lang.GlobalServerNotFound);
                Debug.WriteLine($"[LOBBY CLIENT] EndpointNotFoundException: {ex.Message}");
                return MatchCreationResultCode.MatchCreation_ServerBusy;
            }
            catch (FaultException ex)
            {
                ResetLobbyClient();
                RaiseConnectionError(Lang.GlobalSystemError, Lang.GlobalServerError);
                Debug.WriteLine($"[LOBBY CLIENT] FaultException: {ex.Message}");
                return MatchCreationResultCode.MatchCreation_UnexpectedError;
            }
            catch (CommunicationException ex)
            {
                ResetLobbyClient();
                RaiseConnectionError(Lang.GlobalSystemError, Lang.GlobalServerUnavailable);
                Debug.WriteLine($"[LOBBY CLIENT] CommunicationException: {ex.Message}");
                return MatchCreationResultCode.MatchCreation_Failure;
            }
            catch (TimeoutException ex)
            {
                ResetLobbyClient();
                RaiseConnectionError(Lang.GlobalSystemError, Lang.GlobalServerTimeout);
                Debug.WriteLine($"[LOBBY CLIENT] TimeoutException: {ex.Message}");
                return MatchCreationResultCode.MatchCreation_Timeout;
            }
            catch (ObjectDisposedException ex)
            {
                ResetLobbyClient();
                RaiseConnectionError(Lang.GlobalSystemError, Lang.GlobalServerError);
                Debug.WriteLine($"[LOBBY CLIENT] ObjectDisposedException: {ex.Message}");
                return MatchCreationResultCode.MatchCreation_UnexpectedError;
            }
            catch (InvalidOperationException ex)
            {
                ResetLobbyClient();
                RaiseConnectionError(Lang.GlobalUnexpectedError, Lang.GlobalServerError);
                Debug.WriteLine($"[LOBBY CLIENT] InvalidOperationException: {ex.Message}");
                return MatchCreationResultCode.MatchCreation_UnexpectedError;
            }
        }

        public async Task<JoinMatchResultCode> JoinLobbyAsync(ArchsVsDinosClient.DTO.UserAccountDTO userAccount, string matchCode)
        {
            try
            {
                EnsureClientIsUsable();

                var request = new ArchsVsDinosClient.LobbyService.JoinLobbyRequest
                {
                    LobbyCode = matchCode,
                    UserId = userAccount.IdPlayer,
                    Nickname = userAccount.Nickname,
                    Username = userAccount.Username
                };

                var response = await connectionGuardian.ExecuteWithThrowAsync(() =>
                    Task.FromResult(lobbyManagerClient.JoinLobby(request)));

                if (response.ResultCode == JoinMatchResultCode.JoinMatch_Success && UserSession.Instance.IsGuest)
                {
                    UserSession.Instance.UpdateUserId(response.UserId);
                    Debug.WriteLine($"[LOBBY CLIENT] Guest assigned UserId: {response.UserId}");
                }

                return response.ResultCode;
            }
            catch (EndpointNotFoundException ex)
            {
                ResetLobbyClient();
                RaiseConnectionError(Lang.GlobalSystemError, Lang.GlobalServerNotFound);
                Debug.WriteLine($"[LOBBY CLIENT] JoinLobby EndpointNotFoundException: {ex.Message}");
                return JoinMatchResultCode.JoinMatch_UnexpectedError;
            }
            catch (FaultException<string> ex)
            {
                ResetLobbyClient();
                RaiseConnectionError(Lang.GlobalSystemError, Lang.GlobalServerError);
                Debug.WriteLine($"[LOBBY CLIENT] JoinLobby FaultException<string>: {ex.Detail}");
                return JoinMatchResultCode.JoinMatch_LobbyFull;
            }
            catch (FaultException ex)
            {
                ResetLobbyClient();
                RaiseConnectionError(Lang.GlobalSystemError, Lang.GlobalServerError);
                Debug.WriteLine($"[LOBBY CLIENT] JoinLobby FaultException: {ex.Message}");
                return JoinMatchResultCode.JoinMatch_UnexpectedError;
            }
            catch (CommunicationException ex)
            {
                ResetLobbyClient();
                RaiseConnectionError(Lang.GlobalSystemError, Lang.GlobalServerUnavailable);
                Debug.WriteLine($"[LOBBY CLIENT] JoinLobby CommunicationException: {ex.Message}");
                return JoinMatchResultCode.JoinMatch_UnexpectedError;
            }
            catch (TimeoutException ex)
            {
                ResetLobbyClient();
                RaiseConnectionError(Lang.GlobalSystemError, Lang.GlobalServerTimeout);
                Debug.WriteLine($"[LOBBY CLIENT] JoinLobby TimeoutException: {ex.Message}");
                return JoinMatchResultCode.JoinMatch_Timeout;
            }
            catch (ObjectDisposedException ex)
            {
                ResetLobbyClient();
                RaiseConnectionError(Lang.GlobalSystemError, Lang.GlobalServerError);
                Debug.WriteLine($"[LOBBY CLIENT] JoinLobby ObjectDisposedException: {ex.Message}");
                return JoinMatchResultCode.JoinMatch_UnexpectedError;
            }
            catch (InvalidOperationException ex)
            {
                ResetLobbyClient();
                RaiseConnectionError(Lang.GlobalUnexpectedError, Lang.GlobalServerError);
                Debug.WriteLine($"[LOBBY CLIENT] JoinLobby InvalidOperationException: {ex.Message}");
                return JoinMatchResultCode.JoinMatch_UnexpectedError;
            }
        }

        public async Task<bool> ConnectToLobbyAsync(string matchCode, string nickname)
        {
            return await connectionGuardian.ExecuteAsync(async () =>
            {
                await Task.Run(() => lobbyManagerClient.ConnectToLobby(matchCode, nickname));
            }, operationName: "ConnectToLobby");
        }

        public async Task<bool> TryReconnectToLobbyAsync(string matchCode, string nickname)
        {
            try
            {
                Debug.WriteLine($"[LOBBY CLIENT] Trying to reconnect to lobby {matchCode}...");

                connectionTimer?.Stop();
                EnsureClientIsUsable();

                bool connected = await connectionGuardian.ExecuteAsync(async () =>
                {
                    await Task.Run(() => lobbyManagerClient.ConnectToLobby(matchCode, nickname));
                }, operationName: "ReconnectToLobby");

                if (connected)
                {
                    connectionTimer?.Start();
                    connectionTimer?.NotifyActivity();
                    StartHeartbeat();
                    Debug.WriteLine($"[LOBBY CLIENT] Reconnected to lobby {matchCode}");
                    return true;
                }

                Debug.WriteLine("[LOBBY CLIENT] Reconnect failed.");
                return false;
            }
            catch (EndpointNotFoundException ex)
            {
                Debug.WriteLine($"[LOBBY CLIENT] Reconnect EndpointNotFoundException: {ex.Message}");
                return false;
            }
            catch (CommunicationException ex)
            {
                Debug.WriteLine($"[LOBBY CLIENT] Reconnect CommunicationException: {ex.Message}");
                return false;
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine($"[LOBBY CLIENT] Reconnect TimeoutException: {ex.Message}");
                return false;
            }
            catch (ObjectDisposedException ex)
            {
                Debug.WriteLine($"[LOBBY CLIENT] Reconnect ObjectDisposedException: {ex.Message}");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"[LOBBY CLIENT] Reconnect InvalidOperationException: {ex.Message}");
                return false;
            }
        }

        public void StartConnectionMonitoring(int timeoutSeconds)
        {
            Debug.WriteLine("[LOBBY CLIENT] Iniciando monitoreo de conexión con heartbeat");
            StartHeartbeat();
        }

        private void StartHeartbeat()
        {
            Debug.WriteLine($"[LOBBY CLIENT] ⚙️ StartHeartbeat llamado - Usuario: {UserSession.Instance.CurrentUser?.Nickname}");

            heartbeatTimer?.Dispose();
            heartbeatFailures = 0;

            heartbeatTimer = new System.Timers.Timer(HeartbeatIntervalMs);
            heartbeatTimer.Elapsed += (s, e) => CheckConnectionWithPing();
            heartbeatTimer.AutoReset = true;
            heartbeatTimer.Start();

            Debug.WriteLine($"[LOBBY CLIENT] ✅ Heartbeat iniciado para {UserSession.Instance.CurrentUser?.Nickname}");
        }

        private void CheckConnectionWithPing()
        {
            try
            {
                if (lobbyManagerClient == null)
                {
                    return;
                }

                var pingTask = Task.Run(() =>
                {
                    try
                    {
                        return lobbyManagerClient.Ping();
                    }
                    catch
                    {
                        return false;
                    }
                });

                bool pingSucceeded = pingTask.Wait(2000) && pingTask.Result;

                if (pingSucceeded)
                {
                    heartbeatFailures = 0;
                    Debug.WriteLine("[LOBBY CLIENT] ✓ Ping OK");
                }
                else
                {
                    heartbeatFailures++;
                    Debug.WriteLine($"[LOBBY CLIENT] ✗ Ping FALLÓ, intentos: {heartbeatFailures}/{MaxHeartbeatFailures}");

                    if (heartbeatFailures >= MaxHeartbeatFailures)
                    {
                        Debug.WriteLine($"[LOBBY CLIENT] ❌ Cliente SIN INTERNET: {UserSession.Instance.CurrentUser?.Nickname}");
                        heartbeatTimer?.Stop();
                        RaiseConnectionLost();
                    }
                }
            }
            catch (Exception ex)
            {
                heartbeatFailures++;
                Debug.WriteLine($"[LOBBY CLIENT] Heartbeat exception: {ex.Message}, fallos: {heartbeatFailures}/{MaxHeartbeatFailures}");

                if (heartbeatFailures >= MaxHeartbeatFailures)
                {
                    Debug.WriteLine($"[LOBBY CLIENT] ❌ Excepción en ping: {UserSession.Instance.CurrentUser?.Nickname}");
                    heartbeatTimer?.Stop();
                    RaiseConnectionLost();
                }
            }
        }

        public void StopConnectionMonitoring()
        {
            heartbeatTimer?.Stop();    
            heartbeatTimer?.Dispose();  
            connectionTimer?.Stop();
            Debug.WriteLine("[LOBBY TIMER] Stopped");
        }

        private void OnConnectionTimeout()
        {
            Task ignoredTask = HandleTimeoutAsync();
        }

        private async Task HandleTimeoutAsync()
        {
            bool entered = await reconnectSemaphore.WaitAsync(0);
            if (!entered)
            {
                return;
            }

            try
            {
                consecutiveTimeoutCount++;

                string matchCode = UserSession.Instance.CurrentMatchCode ?? string.Empty;
                string nickname = UserSession.Instance.CurrentUser?.Nickname ?? string.Empty;

                bool reconnected = await TryReconnectToLobbyAsync(matchCode, nickname);
                if (reconnected)
                {
                    consecutiveTimeoutCount = 0;
                    return;
                }

                if (consecutiveTimeoutCount >= MaxConsecutiveTimeouts)
                {
                    RaiseConnectionLost();
                }
            }
            finally
            {
                reconnectSemaphore.Release();
            }
        }

        private void EnsureClientIsUsable()
        {
            if (lobbyManagerClient == null)
            {
                ResetLobbyClient();
                return;
            }

            var state = ((ICommunicationObject)lobbyManagerClient).State;
            if (state == CommunicationState.Faulted || state == CommunicationState.Closed)
            {
                ResetLobbyClient();
            }
        }

        private void ResetLobbyClient()
        {
            if (lobbyManagerClient is ICommunicationObject comm)
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
                catch
                {
                    comm.Abort();
                }
            }

            var synchronizationContext = SynchronizationContext.Current;
            var instanceContext = new InstanceContext(lobbyCallbackManager);

            if (synchronizationContext != null)
            {
                instanceContext.SynchronizationContext = synchronizationContext;
            }

            lobbyManagerClient = new LobbyManagerClient(instanceContext);
            connectionGuardian.MonitorClientState(lobbyManagerClient);

            if (connectionTimer != null)
            {
                lobbyCallbackManager.SetConnectionTimer(connectionTimer);
            }
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

        private void RaiseConnectionError(string title, string message)
        {
            if (uiContext != null)
            {
                uiContext.Post(_ => ConnectionError?.Invoke(title, message), null);
                return;
            }

            Application.Current?.Dispatcher?.Invoke(() => ConnectionError?.Invoke(title, message));
        }

        public Task LeaveLobbyAsync()
        {
            string lobbyCode = UserSession.Instance.CurrentMatchCode ?? string.Empty;

            List<string> identifiers = BuildDisconnectIdentifiers();
            if (string.IsNullOrWhiteSpace(lobbyCode) || identifiers.Count == 0)
            {
                return Task.CompletedTask;
            }

            return LeaveLobbyInternalAsync(lobbyCode, identifiers);
        }

        private async Task LeaveLobbyInternalAsync(string lobbyCode, List<string> identifiers)
        {
            EnsureClientIsUsable();

            foreach (string id in identifiers)
            {
                await connectionGuardian.ExecuteAsync(
                    operation: async () =>
                    {
                        await Task.Run(() => lobbyManagerClient.DisconnectFromLobby(lobbyCode, id));
                    },
                    operationName: "DisconnectFromLobby",
                    suppressErrors: true
                );
            }
        }

        private static List<string> BuildDisconnectIdentifiers()
        {
            var identifiers = new List<string>();

            string nickname = UserSession.Instance.GetNickname();
            if (!string.IsNullOrWhiteSpace(nickname))
            {
                identifiers.Add(nickname);
            }

            string username = UserSession.Instance.CurrentUser?.Username;
            if (!string.IsNullOrWhiteSpace(username) &&
                !identifiers.Any(x => string.Equals(x, username, StringComparison.OrdinalIgnoreCase)))
            {
                identifiers.Add(username);
            }

            return identifiers;
        }


        public void LeaveLobby(string username)
        {
            _ = LeaveLobbyAsync();
        }


        public void StartGame(string matchCode)
        {
            Task ignoredTask = connectionGuardian.ExecuteAsync(async () =>
            {
                await Task.Run(() => lobbyManagerClient.StartGame(matchCode, UserSession.Instance.CurrentUser.IdUser));
            });
        }

        public async Task<bool> SendLobbyInviteByEmail(string email, string matchCode, string senderUsername)
        {
            try
            {
                string[] guestsList = new string[] { email };

                var result = await connectionGuardian.ExecuteAsync(async () =>
                {
                    return await Task.Run(() => lobbyManagerClient.SendInvitations(matchCode, senderUsername, guestsList));
                });

                return result;
            }
            catch (EndpointNotFoundException endpointEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendInvite EndpointNotFoundException: {endpointEx.Message}");
                MessageBox.Show(Lang.GlobalServerNotFound, Lang.GlobalError, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (FaultException faultEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendInvite FaultException: {faultEx.Message}");
                MessageBox.Show(Lang.GlobalServerError, Lang.GlobalError, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (CommunicationException commEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendInvite CommunicationException: {commEx.Message}");
                MessageBox.Show(Lang.GlobalServerUnavailable, Lang.GlobalError, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (TimeoutException timeoutEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendInvite TimeoutException: {timeoutEx.Message}");
                MessageBox.Show(Lang.GlobalServerTimeout, Lang.GlobalError, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (FormatException formatEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendInvite FormatException: {formatEx.Message}");
                MessageBox.Show(Lang.Register_InvalidEmail, Lang.GlobalError, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            catch (ArgumentNullException argEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendInvite ArgumentNullException: {argEx.Message}");
                MessageBox.Show(Lang.Authentication_UnexpectedError, Lang.GlobalError, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        public void KickPlayer(string lobbyCode, int hostUserId, string targetNickname)
        {
            Task ignoredTask = connectionGuardian.ExecuteAsync(async () =>
            {
                await Task.Run(() => lobbyManagerClient.KickPlayer(lobbyCode, hostUserId, targetNickname));
            });
        }

        public async Task<bool> SendLobbyInviteToFriendAsync(string lobbyCode, string senderNickname, string targetUsername)
        {
            try
            {
                var result = await connectionGuardian.ExecuteAsync(async () =>
                {
                    return await lobbyManagerClient.SendLobbyInviteToFriendAsync(lobbyCode, senderNickname, targetUsername);
                });

                return result;
            }
            catch (EndpointNotFoundException endpointEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendLobbyInviteToFriend EndpointNotFoundException: {endpointEx.Message}");
                ConnectionError?.Invoke(Lang.GlobalSystemError, Lang.GlobalServerNotFound);
                return false;
            }
            catch (FaultException faultEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendLobbyInviteToFriend FaultException: {faultEx.Message}");
                ConnectionError?.Invoke(Lang.GlobalSystemError, Lang.GlobalServerError);
                return false;
            }
            catch (CommunicationException commEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendLobbyInviteToFriend CommunicationException: {commEx.Message}");
                ConnectionError?.Invoke(Lang.GlobalSystemError, Lang.GlobalServerUnavailable);
                return false;
            }
            catch (TimeoutException timeoutEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendLobbyInviteToFriend TimeoutException: {timeoutEx.Message}");
                ConnectionError?.Invoke(Lang.GlobalSystemError, Lang.GlobalServerTimeout);
                return false;
            }
            catch (ObjectDisposedException objEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendLobbyInviteToFriend ObjectDisposedException: {objEx.Message}");
                ConnectionError?.Invoke(Lang.GlobalSystemError, Lang.GlobalServerError);
                return false;
            }
            catch (InvalidOperationException invEx)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendLobbyInviteToFriend InvalidOperationException: {invEx.Message}");
                ConnectionError?.Invoke(Lang.GlobalUnexpectedError, Lang.GlobalServerError);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOBBY CLIENT] SendLobbyInviteToFriend Exception: {ex.Message}");
                ConnectionError?.Invoke(Lang.GlobalUnexpectedError, Lang.GlobalServerError);
                return false;
            }
        }


    }
}
