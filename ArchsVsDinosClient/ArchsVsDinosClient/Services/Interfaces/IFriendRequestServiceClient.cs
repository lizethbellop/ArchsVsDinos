using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services.Interfaces
{
    public interface IFriendRequestServiceClient : IDisposable
    {
        event Action<string, string> ConnectionError;
        event Action ServerReconnected;
        event Action<bool> FriendRequestSent;
        event Action<bool> FriendRequestAccepted;
        event Action<bool> FriendRequestRejected;
        event Action<string[]> PendingRequestsReceived;
        event Action<string[]> SentRequestsReceived;
        event Action<string> FriendRequestReceived;

        Task SendFriendRequestAsync(string fromUser, string toUser);
        Task AcceptFriendRequestAsync(string fromUser, string toUser);
        Task RejectFriendRequestAsync(string fromUser, string toUser);
        Task GetPendingRequestsAsync(string username);
        Task GetSentRequestsAsync(string username); 
        Task SubscribeAsync(string username);
        Task UnsubscribeAsync(string username);
    }


}
