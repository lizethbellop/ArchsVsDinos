using ArchsVsDinosClient.ChatManager;
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
    public class ChatServiceClient : IChatServiceClient
    {
        private readonly ChatManagerClient client;
        private readonly InstanceContext context;
        private readonly ChatCallbackHandler callback;
        private readonly SynchronizationContext syncContext;
        private bool isDisposed;

        public event Action<string, string, string> MessageReceived;
        public event Action<ChatResultCode, string> SystemNotificationReceived;
        public event Action<List<string>> UserListUpdated;

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
        }

        public async Task ConnectAsync(string username)
        {
            await Task.Run(() => client.Connect(username)).ConfigureAwait(false);
        }

        public async Task SendMessageAsync(string message, string username)
        {
            await Task.Run(() => client.SendMessageToRoom(message, username)).ConfigureAwait(false);
        }

        public async Task DisconnectAsync(string username)
        {
            await Task.Run(() => client.Disconnect(username)).ConfigureAwait(false);
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
                    callback.MessageReceived -= OnMessageReceived;
                    callback.SystemNotificationReceived -= OnSystemNotificationReceived;
                    callback.UserListUpdated -= OnUserListUpdated;
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
