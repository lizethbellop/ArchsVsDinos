using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchsVsDinosClient.FriendRequestService;
using ArchsVsDinosClient.Services.Interfaces;

namespace ArchsVsDinosClient.Services
{
    public class FriendRequestCallbackHandler : IFriendRequestManagerCallback
    {
        public event Action<bool> FriendRequestSent;
        public event Action<bool> FriendRequestAccepted;
        public event Action<bool> FriendRequestRejected;
        public event Action<string[]> PendingRequestsReceived;
        public event Action<string[]> SentRequestsReceived;
        public event Action<string> FriendRequestReceived;

        public void OnFriendRequestSent(bool success)
        {
            FriendRequestSent?.Invoke(success);
        }

        public void OnFriendRequestAccepted(bool success)
        {
            FriendRequestAccepted?.Invoke(success);
        }

        public void OnFriendRequestRejected(bool success)
        {
            FriendRequestRejected?.Invoke(success);
        }

        public void OnPendingRequestsReceived(string[] requests)
        {
            string[] requestsArray = requests ?? new string[0];
            PendingRequestsReceived?.Invoke(requestsArray);
        }

        public void OnSentRequestsReceived(string[] requests)
        {
            string[] requestsArray = requests ?? new string[0];
            SentRequestsReceived?.Invoke(requestsArray);
        }

        public void OnFriendRequestReceived(string fromUser)
        {
            FriendRequestReceived?.Invoke(fromUser);
        }
    }
}
