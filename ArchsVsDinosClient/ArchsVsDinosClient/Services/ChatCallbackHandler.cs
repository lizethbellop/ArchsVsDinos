using ArchsVsDinosClient.ChatManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services
{
    public class ChatCallbackHandler : IChatManagerCallback
    {
        public event Action<string, string, string> MessageReceived;
        public event Action<ChatResultCode, string> SystemNotificationReceived;
        public event Action<List<string>> UserListUpdated;

        public void ReceiveMessage(string roomId, string fromUser, string message)
        {
            MessageReceived?.Invoke(roomId, fromUser, message);
        }

        public void ReceiveSystemNotification(ChatResultCode code, string notification)
        {
            SystemNotificationReceived?.Invoke(code, notification);
        }

        public void UpdateUserList(List<string> users)
        {
            UserListUpdated?.Invoke(users);
        }

        public void UpdateUserList(string[] users)
        {
            UserListUpdated?.Invoke(users?.ToList() ?? new List<string>());
        }
    }
}
