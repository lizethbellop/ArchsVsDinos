using ArchsVsDinosClient.FriendRequestService;
using ArchsVsDinosClient.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services
{
    public class FriendRequestServiceClient : IFriendRequestServiceClient
    {
        private readonly FriendRequestManagerClient client;
        private readonly InstanceContext context;
        private readonly FriendRequestCallbackHandler callback;
        private readonly SynchronizationContext syncContext;
        private bool isDisposed;

        public event Action<bool> FriendRequestSent;
        public event Action<bool> FriendRequestAccepted;
        public event Action<bool> FriendRequestRejected;
        public event Action<string[]> PendingRequestsReceived;
        public event Action<string> FriendRequestReceived;

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
        }

        public void SendFriendRequest(string fromUser, string toUser)
        {
            client.SendFriendRequest(fromUser, toUser);
        }

        public void AcceptFriendRequest(string fromUser, string toUser)
        {
            client.AcceptFriendRequest(fromUser, toUser);
        }

        public void RejectFriendRequest(string fromUser, string toUser)
        {
            client.RejectFriendRequest(fromUser, toUser);
        }

        public void GetPendingRequests(string username)
        {
            client.GetPendingRequests(username);
        }

        public void Subscribe(string username)
        {
            client.Subscribe(username);
        }

        public void Unsubscribe(string username)
        {
            client.Unsubscribe(username);
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
            if (isDisposed)
            {
                return;
            }

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
                        {
                            client.Close();
                        }
                        else if (client.State == CommunicationState.Faulted)
                        {
                            client.Abort();
                        }
                    }
                    catch (CommunicationException)
                    {
                        client.Abort();
                    }
                    catch (TimeoutException)
                    {
                        client.Abort();
                    }
                }
            }

            isDisposed = true;
        }
    }
}
