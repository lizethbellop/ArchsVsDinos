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

            InitializeClient();

            guardian = new WcfConnectionGuardian(
                onError: OnConnectionError,
                logger: new Logger()
            );

            guardian.ServerStateChanged += OnServerStateChanged;
            guardian.MonitorClientState(client);
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
            }
        }

        private bool IsClientUsable()
        {
            return client != null && client.State == CommunicationState.Opened;
        }

        private void EnsureClientIsUsable()
        {
            lock (clientLock)
            {
                if (client == null ||
                    client.State == CommunicationState.Closed ||
                    client.State == CommunicationState.Faulted)
                {
                    try { client?.Abort(); } catch { }
                    InitializeClient();
                    guardian.MonitorClientState(client);
                }
            }
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
                    guardian.MonitorClientState(client);

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

        public void SendFriendRequest(string fromUser, string toUser)
        {
            _ = guardian.ExecuteAsync(() =>
            {
                EnsureClientIsUsable();
                client.SendFriendRequest(fromUser, toUser);
                return Task.CompletedTask;
            });
        }

        public void AcceptFriendRequest(string fromUser, string toUser)
        {
            _ = guardian.ExecuteAsync(() =>
            {
                EnsureClientIsUsable();
                client.AcceptFriendRequest(fromUser, toUser);
                return Task.CompletedTask;
            });
        }

        public void RejectFriendRequest(string fromUser, string toUser)
        {
            _ = guardian.ExecuteAsync(() =>
            {
                EnsureClientIsUsable();
                client.RejectFriendRequest(fromUser, toUser);
                return Task.CompletedTask;
            });
        }

        public void GetPendingRequests(string username)
        {
            _ = guardian.ExecuteAsync(() =>
            {
                EnsureClientIsUsable();
                client.GetPendingRequests(username);
                return Task.CompletedTask;
            });
        }

        public async Task Subscribe(string username)
        {
            currentUsername = username;

            await guardian.ExecuteAsync(() =>
            {
                EnsureClientIsUsable();
                client.Subscribe(username);
                return Task.CompletedTask;
            });
        }

        public void Unsubscribe(string username)
        {
            if (!IsClientUsable())
            {
                currentUsername = null;
                return;
            }

            try
            {
                client.Unsubscribe(username);
                currentUsername = null;
            }
            catch { }
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
