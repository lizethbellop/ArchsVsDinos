using ArchsVsDinosClient.ChatManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services.Interfaces
{
    public interface IChatServiceClient : IDisposable
    {
        event Action<string, string, string> MessageReceived;
        event Action<ChatResultCode, string> SystemNotificationReceived;
        event Action<List<string>> UserListUpdated;
        event Action<string, string> ConnectionError;
        event Action<string, int> UserBanned;
        event Action<string, string> UserExpelled;
        event Action<string> LobbyClosed;

       Task ConnectAsync(string username, int context = 0, string matchCode = null);

        Task SendMessageAsync(string message, string username);
        Task DisconnectAsync(string username);
    }
}
