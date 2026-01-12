using ArchsVsDinosClient.FriendRequestService;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArchsVsDinosClient.Logging;

namespace ArchsVsDinosClient.Services
{
    public class FriendRequestServiceClient : IFriendRequestServiceClient
    {
        private FriendRequestManagerClient client;
        private InstanceContext context;
        private readonly FriendRequestCallbackHandler callback;
        private readonly WcfConnectionGuardian guardian;
        private readonly SynchronizationContext syncContext;
        private readonly object clientLock = new object();
        private bool isDisposed;

        private string currentUsername;
        private Timer reconnectionTimer;
        private int reconnectionAttempts;
        private const int MaxReconnectionAttempts = 5;
        private const int ReconnectionIntervalMs = 5000;

        public event Action<bool> FriendRequestSent;
        public event Action<bool> FriendRequestAccepted;
        public event Action<bool> FriendRequestRejected;
        public event Action<string[]> PendingRequestsReceived;
        public event Action<string> FriendRequestReceived;
        public event Action<string, string> ConnectionError;
        public event Action ServerReconnected;

        public FriendRequestServiceClient()
        {
            syncContext = SynchronizationContext.Current;
            callback = new FriendRequestCallbackHandler();

            callback.FriendRequestSent += OnFriendRequestSent;
            callback.FriendRequestAccepted += OnFriendRequestAccepted;
            callback.FriendRequestRejected += OnFriendRequestRejected;
            callback.PendingRequestsReceived += OnPendingRequestsReceived;
            callback.FriendRequestReceived += OnFriendRequestReceived;

            guardian = new WcfConnectionGuardian(
                onError: OnConnectionError,
                logger: new Logger()
            );

            try
            {
                InitializeClient();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing: {ex.Message}");
            }

            guardian.ServerStateChanged += OnServerStateChanged;
        }

        private void InitializeClient()
        {
            lock (clientLock)
            {
                context = new InstanceContext(callback)
                {
                    SynchronizationContext = syncContext
                };

                client = new FriendRequestManagerClient(context);
                guardian.MonitorClientState(client);
            }
        }

        private bool IsClientUsable()
        {
            try
            {
                return client != null && client.State == CommunicationState.Opened;
            }
            catch
            {
                return false;
            }
        }

        public async Task SendFriendRequestAsync(string fromUser, string toUser)
        {
            await guardian.ExecuteWithThrowAsync<bool>(
                () =>
                {
                    client.SendFriendRequest(fromUser, toUser);
                    return Task.FromResult(true);
                },
                operationName: "enviar solicitud de amistad"
            );
        }

        public async Task AcceptFriendRequestAsync(string fromUser, string toUser)
        {
            await guardian.ExecuteWithThrowAsync<bool>(
                () =>
                {
                    client.AcceptFriendRequest(fromUser, toUser);
                    return Task.FromResult(true);
                },
                operationName: "aceptar solicitud de amistad"
            );
        }

        public async Task RejectFriendRequestAsync(string fromUser, string toUser)
        {
            await guardian.ExecuteWithThrowAsync<bool>(
                () =>
                {
                    client.RejectFriendRequest(fromUser, toUser);
                    return Task.FromResult(true);
                },
                operationName: "rechazar solicitud de amistad"
            );
        }

        public async Task GetPendingRequestsAsync(string username)
        {
            await guardian.ExecuteWithThrowAsync<bool>(
                () =>
                {
                    client.GetPendingRequests(username);
                    return Task.FromResult(true);
                },
                operationName: "obtener solicitudes pendientes"
            );
        }

        public async Task SubscribeAsync(string username)
        {
            currentUsername = username;

            await guardian.ExecuteWithThrowAsync<bool>(
                () =>
                {
                    client.Subscribe(username);
                    return Task.FromResult(true);
                },
                operationName: "suscribirse al servicio"
            );
        }

        public async Task UnsubscribeAsync(string username)
        {
            if (!IsClientUsable())
            {
                currentUsername = null;
                return;
            }

            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        client.Unsubscribe(username);
                    }
                    catch (CommunicationException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FRIEND SERVICE] Sin conexión al desuscribir: {ex.Message}");
                    }
                    catch (TimeoutException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FRIEND SERVICE] Timeout al desuscribir: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FRIEND SERVICE] Error al desuscribir: {ex.Message}");
                    }
                });

                currentUsername = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FRIEND SERVICE] Error general desuscribiendo: {ex.Message}");
                currentUsername = null;
            }
        }

        public void SendFriendRequest(string fromUser, string toUser)
        {
            Task.Run(async () => await SendFriendRequestAsync(fromUser, toUser));
        }

        public void AcceptFriendRequest(string fromUser, string toUser)
        {
            Task.Run(async () => await AcceptFriendRequestAsync(fromUser, toUser));
        }

        public void RejectFriendRequest(string fromUser, string toUser)
        {
            Task.Run(async () => await RejectFriendRequestAsync(fromUser, toUser));
        }

        public void GetPendingRequests(string username)
        {
            Task.Run(async () => await GetPendingRequestsAsync(username));
        }

        public void Unsubscribe(string username)
        {
            Task.Run(async () => await UnsubscribeAsync(username));
        }

        private void OnServerStateChanged(object sender, ServerStateChangedEventArgs e)
        {
            if (!e.IsAvailable)
                StartReconnectionProcess();
            else
                StopReconnectionProcess();
        }

        private void StartReconnectionProcess()
        {
            StopReconnectionProcess();
            reconnectionAttempts = 0;

            reconnectionTimer = new Timer(
                TryReconnect,
                null,
                ReconnectionIntervalMs,
                ReconnectionIntervalMs
            );
        }

        private void StopReconnectionProcess()
        {
            reconnectionTimer?.Dispose();
            reconnectionTimer = null;
            reconnectionAttempts = 0;
        }

        private void TryReconnect(object state)
        {
            if (reconnectionAttempts >= MaxReconnectionAttempts)
            {
                StopReconnectionProcess();
                return;
            }

            reconnectionAttempts++;

            lock (clientLock)
            {
                try
                {
                    client?.Abort();
                    InitializeClient();

                    if (!string.IsNullOrEmpty(currentUsername))
                    {
                        client.Subscribe(currentUsername);
                        client.GetPendingRequests(currentUsername);

                        guardian.RestoreNormalMode();
                        StopReconnectionProcess();
                        ServerReconnected?.Invoke();
                    }
                }
                catch { }
            }
        }

        private void OnConnectionError(string title, string message)
        {
            ConnectionError?.Invoke(title, message);
        }

        private void OnFriendRequestSent(bool success) =>
            FriendRequestSent?.Invoke(success);

        private void OnFriendRequestAccepted(bool success) =>
            FriendRequestAccepted?.Invoke(success);

        private void OnFriendRequestRejected(bool success) =>
            FriendRequestRejected?.Invoke(success);

        private void OnPendingRequestsReceived(string[] requests) =>
            PendingRequestsReceived?.Invoke(requests);

        private void OnFriendRequestReceived(string fromUser) =>
            FriendRequestReceived?.Invoke(fromUser);

        public void Dispose()
        {
            if (isDisposed) return;

            StopReconnectionProcess();

            lock (clientLock)
            {
                try
                {
                    if (client?.State == CommunicationState.Opened)
                        client.Close();
                    else
                        client?.Abort();
                }
                catch { }
            }

            isDisposed = true;
        }
    }

}
