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
        private bool isDisposed;
        private readonly object clientLock = new object();

        private string currentUsername;
        private Timer reconnectionTimer;
        private int reconnectionAttempts = 0;
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
                try
                {
                    context = new InstanceContext(callback);
                    context.SynchronizationContext = syncContext;
                    client = new FriendRequestManagerClient(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al inicializar cliente: {ex.Message}");
                }
            }
        }

        private bool IsClientUsable()
        {
            lock (clientLock)
            {
                return client != null &&
                       client.State == CommunicationState.Opened;
            }
        }

        private void EnsureClientIsUsable()
        {
            lock (clientLock)
            {
                if (client == null ||
                    client.State == CommunicationState.Closed ||
                    client.State == CommunicationState.Faulted)
                {
                    try
                    {
                        client?.Abort();
                    }
                    catch { }

                    InitializeClient();
                    guardian.MonitorClientState(client);
                }
            }
        }

        private void OnServerStateChanged(object sender, ServerStateChangedEventArgs e)
        {
            if (!e.IsAvailable)
            {
                StartReconnectionProcess();
            }
            else
            {
                StopReconnectionTimer();
                reconnectionAttempts = 0;
            }
        }

        private void StartReconnectionProcess()
        {
            StopReconnectionTimer();
            reconnectionAttempts = 0;

            reconnectionTimer = new Timer(
                TryReconnect,
                null,
                ReconnectionIntervalMs,
                ReconnectionIntervalMs
            );
        }

        private void StopReconnectionTimer()
        {
            reconnectionTimer?.Dispose();
            reconnectionTimer = null;
        }

        private async void TryReconnect(object state)
        {
            if (reconnectionAttempts >= MaxReconnectionAttempts)
            {
                StopReconnectionTimer();
                Console.WriteLine("Máximo de intentos de reconexión alcanzado");
                return;
            }

            reconnectionAttempts++;
            Console.WriteLine($"Intento de reconexión {reconnectionAttempts}/{MaxReconnectionAttempts}");

            lock (clientLock)
            {
                try
                {
                    if (client != null)
                    {
                        try
                        {
                            client.Abort();
                        }
                        catch { }
                    }

                    InitializeClient();
                    guardian.MonitorClientState(client);

                    if (!string.IsNullOrEmpty(currentUsername))
                    {
                        client.Subscribe(currentUsername);

                        Console.WriteLine("Reconexión exitosa");
                        guardian.RestoreNormalMode();
                        StopReconnectionTimer();
                        reconnectionAttempts = 0;

                        ServerReconnected?.Invoke();

                        client.GetPendingRequests(currentUsername);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fallo en reconexión: {ex.Message}");
                }
            }
        }

        private void OnConnectionError(string title, string message)
        {
            ConnectionError?.Invoke(title, message);
        }

        public void SendFriendRequest(string fromUser, string toUser)
        {
            _ = guardian.ExecuteAsync(
                async () =>
                {
                    EnsureClientIsUsable();
                    await Task.Run(() => client.SendFriendRequest(fromUser, toUser));
                },
                operationName: "enviar solicitud de amistad"
            );
        }

        public void AcceptFriendRequest(string fromUser, string toUser)
        {
            _ = guardian.ExecuteAsync(
                async () =>
                {
                    EnsureClientIsUsable();
                    await Task.Run(() => client.AcceptFriendRequest(fromUser, toUser));
                },
                operationName: "aceptar solicitud de amistad"
            );
        }

        public void RejectFriendRequest(string fromUser, string toUser)
        {
            _ = guardian.ExecuteAsync(
                async () =>
                {
                    EnsureClientIsUsable();
                    await Task.Run(() => client.RejectFriendRequest(fromUser, toUser));
                },
                operationName: "rechazar solicitud de amistad"
            );
        }

        public void GetPendingRequests(string username)
        {
            _ = guardian.ExecuteAsync(
                async () =>
                {
                    EnsureClientIsUsable();
                    await Task.Run(() => client.GetPendingRequests(username));
                },
                operationName: "obtener solicitudes pendientes"
            );
        }

        public void Subscribe(string username)
        {
            currentUsername = username;

            _ = guardian.ExecuteAsync(
                async () =>
                {
                    EnsureClientIsUsable();
                    await Task.Run(() => client.Subscribe(username));
                },
                operationName: "suscribir a notificaciones"
            );
        }

        public void Unsubscribe(string username)
        {
            // Si el cliente no está usable, no hacer nada
            if (!IsClientUsable())
            {
                currentUsername = null;
                return;
            }

            // Hacerlo directo con try-catch, sin guardián ni async
            try
            {
                lock (clientLock)
                {
                    if (IsClientUsable())
                    {
                        client.Unsubscribe(username);
                        currentUsername = null;
                    }
                }
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"Cliente ya desechado al desuscribir: {ex.Message}");
                currentUsername = null;
            }
            catch (CommunicationException ex)
            {
                Console.WriteLine($"Error de comunicación al desuscribir: {ex.Message}");
                currentUsername = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al desuscribir: {ex.Message}");
                currentUsername = null;
            }
        }

        private void OnFriendRequestSent(bool success)
        {
            FriendRequestSent?.Invoke(success);
        }

        private void OnFriendRequestAccepted(bool success)
        {
            FriendRequestAccepted?.Invoke(success);
        }

        private void OnFriendRequestRejected(bool success)
        {
            FriendRequestRejected?.Invoke(success);
        }

        private void OnPendingRequestsReceived(string[] requests)
        {
            PendingRequestsReceived?.Invoke(requests);
        }

        private void OnFriendRequestReceived(string fromUser)
        {
            FriendRequestReceived?.Invoke(fromUser);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                StopReconnectionTimer();

                lock (clientLock)
                {
                    if (callback != null)
                    {
                        callback.FriendRequestSent -= OnFriendRequestSent;
                        callback.FriendRequestAccepted -= OnFriendRequestAccepted;
                        callback.FriendRequestRejected -= OnFriendRequestRejected;
                        callback.PendingRequestsReceived -= OnPendingRequestsReceived;
                        callback.FriendRequestReceived -= OnFriendRequestReceived;
                    }

                    if (client != null)
                    {
                        try
                        {
                            if (client.State == CommunicationState.Opened)
                                client.Close();
                            else
                                client.Abort();
                        }
                        catch
                        {
                            client.Abort();
                        }
                    }
                }
            }

            isDisposed = true;
        }
    }
}
