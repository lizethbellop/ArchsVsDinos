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
        private readonly FriendRequestManagerClient client;
        private readonly InstanceContext context;
        private readonly FriendRequestCallbackHandler callback;
        private readonly WcfConnectionGuardian guardian;
        private readonly SynchronizationContext syncContext;
        private bool isDisposed;

        public event Action<bool> FriendRequestSent;
        public event Action<bool> FriendRequestAccepted;
        public event Action<bool> FriendRequestRejected;
        public event Action<string[]> PendingRequestsReceived;
        public event Action<string> FriendRequestReceived;
        public event Action<string, string> ConnectionError;

        public FriendRequestServiceClient()
        {
            syncContext = SynchronizationContext.Current;
            callback = new FriendRequestCallbackHandler();

            callback.FriendRequestSent += OnFriendRequestSent;
            callback.FriendRequestAccepted += OnFriendRequestAccepted;
            callback.FriendRequestRejected += OnFriendRequestRejected;
            callback.PendingRequestsReceived += OnPendingRequestsReceived;
            callback.FriendRequestReceived += OnFriendRequestReceived;

            context = new InstanceContext(callback);
            context.SynchronizationContext = syncContext;

            client = new FriendRequestManagerClient(context);

            guardian = new WcfConnectionGuardian(
                onError: (title, msg) => ConnectionError?.Invoke(title, msg),
                logger: new Logger()
            );
            guardian.MonitorClientState(client);
        }

        public void SendFriendRequest(string fromUser, string toUser)
        {
            _ = guardian.ExecuteAsync(
            async () => await System.Threading.Tasks.Task.Run(() => client.SendFriendRequest(fromUser, toUser)),
            operationName: "enviar solicitud de amistad"
            );
        }

        public void AcceptFriendRequest(string fromUser, string toUser)
        {
            _ = guardian.ExecuteAsync(
            async () => await System.Threading.Tasks.Task.Run(() => client.AcceptFriendRequest(fromUser, toUser)),
            operationName: "aceptar solicitud de amistad"
            );
        }

        public void RejectFriendRequest(string fromUser, string toUser)
        {
            _ = guardian.ExecuteAsync(
            async () => await System.Threading.Tasks.Task.Run(() => client.RejectFriendRequest(fromUser, toUser)),
            operationName: "rechazar solicitud de amistad"
            );
        }

        public void GetPendingRequests(string username)
        {
            _ = guardian.ExecuteAsync(
            async () => await System.Threading.Tasks.Task.Run(() => client.GetPendingRequests(username)),
            operationName: "obtener solicitudes pendientes"
            );
        }

        public void Subscribe(string username)
        {
            _ = guardian.ExecuteAsync(
            async () => await System.Threading.Tasks.Task.Run(() => client.Subscribe(username)),
            operationName: "suscribir a notificaciones"
            );
        }

        public void Unsubscribe(string username)
        {
            _ = guardian.ExecuteAsync(
            async () => await System.Threading.Tasks.Task.Run(() => client.Unsubscribe(username)),
            operationName: "desuscribir de notificaciones"
            );
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
                        else if (client.State == CommunicationState.Faulted)
                            client.Abort();
                    }
                    catch (CommunicationException)
                    {
                        client.Abort();
                    }
                    catch (TimeoutException)
                    {
                        client.Abort();
                    }
                    catch
                    {
                        client.Abort();
                    }
                }
            }

            isDisposed = true;
        }
    }
}
