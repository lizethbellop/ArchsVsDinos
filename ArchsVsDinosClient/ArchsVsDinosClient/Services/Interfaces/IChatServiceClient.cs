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

        Task ConnectAsync(string username);
        Task SendMessageAsync(string message, string username);
        Task DisconnectAsync(string username);
    }
}
