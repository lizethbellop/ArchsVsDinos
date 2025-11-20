using ArchsVsDinosClient.ChatManager;
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
    public class ChatServiceClient : IChatServiceClient
    {
        private readonly ChatManagerClient client;
        private readonly InstanceContext context;
        private readonly ChatCallbackHandler callback;
        private readonly WcfConnectionGuardian guardian;
        private readonly SynchronizationContext syncContext;
        private bool isDisposed;

        public event Action<string, string, string> MessageReceived;
        public event Action<ChatResultCode, string> SystemNotificationReceived;
        public event Action<List<string>> UserListUpdated;
        public event Action<string, string> ConnectionError;

        public ChatServiceClient()
        {
            syncContext = SynchronizationContext.Current;
            callback = new ChatCallbackHandler();
            callback.MessageReceived += OnMessageReceived;
            callback.SystemNotificationReceived += OnSystemNotificationReceived;
            callback.UserListUpdated += OnUserListUpdated;

            context = new InstanceContext(callback);
            context.SynchronizationContext = syncContext;
            client = new ChatManagerClient(context);

            guardian = new WcfConnectionGuardian(
                onError: (title, msg) => ConnectionError?.Invoke(title, msg),
                logger: new Logger()
            );
            guardian.MonitorClientState(client);
        }

        public async Task ConnectAsync(string username)
        {
            await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.Connect(username)),
                operationName: "conexión"
            );
        }

        public async Task SendMessageAsync(string message, string username)
        {
            await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.SendMessageToRoom(message, username)),
                operationName: "envío de mensaje"
            );
        }

        public async Task DisconnectAsync(string username)
        {
            await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.Disconnect(username)),
                operationName: "desconexión"
            );
        }

        private void OnMessageReceived(string roomId, string fromUser, string message)
        {
            MessageReceived?.Invoke(roomId, fromUser, message);
        }

        private void OnSystemNotificationReceived(ChatResultCode code, string notification)
        {
            SystemNotificationReceived?.Invoke(code, notification);
        }

        private void OnUserListUpdated(List<string> users)
        {
            UserListUpdated?.Invoke(users);
        }

        public void Dispose()
        {
            if (isDisposed) return;
            if (callback != null)
            {
                callback.MessageReceived -= OnMessageReceived;
                callback.SystemNotificationReceived -= OnSystemNotificationReceived;
                callback.UserListUpdated -= OnUserListUpdated;
            }
            try
            {
                if (client?.State == CommunicationState.Opened)
                    client.Close();
                else if (client?.State == CommunicationState.Faulted)
                    client.Abort();
            }
            catch { client?.Abort(); }
            isDisposed = true;
        }
    }
}
