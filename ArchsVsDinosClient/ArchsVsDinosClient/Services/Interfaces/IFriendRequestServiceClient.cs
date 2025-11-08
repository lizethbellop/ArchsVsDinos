using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services.Interfaces
{
    public interface IFriendRequestServiceClient : IDisposable
    {
        void SendFriendRequest(string fromUser, string toUser);
        void AcceptFriendRequest(string fromUser, string toUser);
        void RejectFriendRequest(string fromUser, string toUser);
        void GetPendingRequests(string username);
        void Subscribe(string username);
        void Unsubscribe(string username);

        event Action<bool> FriendRequestSent;
        event Action<bool> FriendRequestAccepted;
        event Action<bool> FriendRequestRejected;
        event Action<string[]> PendingRequestsReceived;
        event Action<string> FriendRequestReceived;
    }
}
