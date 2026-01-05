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
        private ChatManagerClient client;
        private InstanceContext context;
        private readonly ChatCallbackHandler callback;
        private readonly WcfConnectionGuardian guardian;
        private readonly SynchronizationContext syncContext;
        private readonly object clientLock = new object();
        private bool isDisposed;

        public event Action<string, string, string> MessageReceived;
        public event Action<ChatResultCode, string> SystemNotificationReceived;
        public event Action<List<string>> UserListUpdated;
        public event Action<string, string> ConnectionError;
        public event Action<string, int> UserBanned;
        public event Action<string, string> UserExpelled;
        public event Action<string> LobbyClosed;

        public ChatServiceClient()
        {
            syncContext = SynchronizationContext.Current;
            callback = new ChatCallbackHandler();

            callback.MessageReceived += OnMessageReceived;
            callback.SystemNotificationReceived += OnSystemNotificationReceived;
            callback.UserListUpdated += OnUserListUpdated;
            callback.UserBanned += OnUserBanned;
            callback.UserExpelled += OnUserExpelled;
            callback.LobbyClosed += OnLobbyClosed;

            InitializeClient();

            guardian = new WcfConnectionGuardian(
                onError: (title, msg) => ConnectionError?.Invoke(title, msg),
                logger: new Logger()
            );

            guardian.MonitorClientState(client);
        }

        private void InitializeClient()
        {
            lock (clientLock)
            {
                context = new InstanceContext(callback)
                {
                    SynchronizationContext = syncContext
                };

                client = new ChatManagerClient(context);
            }
        }

        private void EnsureClientIsUsable()
        {
            lock (clientLock)
            {
                if (client == null ||
                    client.State == CommunicationState.Closed ||
                    client.State == CommunicationState.Faulted)
                {
                    try { client?.Abort(); } catch { }
                    InitializeClient();
                    guardian.MonitorClientState(client);
                }
            }
        }

        public async Task ConnectAsync(string username, int contextType = 0, string matchCode = null)
        {
            await guardian.ExecuteAsync(() =>
            {
                EnsureClientIsUsable();

                var request = new ChatConnectionRequest
                {
                    Username = username,
                    Context = contextType,   
                    MatchCode = matchCode
                };

                client.Connect(request);
                return Task.CompletedTask;
            }, operationName: "connection to chat");
        }

        public async Task SendMessageAsync(string message, string username)
        {
            await guardian.ExecuteAsync(() =>
            {
                EnsureClientIsUsable();
                client.SendMessageToRoom(message, username);
                return Task.CompletedTask;
            }, operationName: "message sending");
        }

        public async Task DisconnectAsync(string username)
        {
            await guardian.ExecuteAsync(() =>
            {
                EnsureClientIsUsable();
                client.Disconnect(username);
                return Task.CompletedTask;
            }, operationName: "desconnection");
        }

        private void OnMessageReceived(string roomId, string fromUser, string message) =>
            MessageReceived?.Invoke(roomId, fromUser, message);

        private void OnSystemNotificationReceived(ChatResultCode code, string notification) =>
            SystemNotificationReceived?.Invoke(code, notification);

        private void OnUserListUpdated(List<string> users) =>
            UserListUpdated?.Invoke(users);

        private void OnUserBanned(string username, int strikes) =>
            UserBanned?.Invoke(username, strikes);

        private void OnUserExpelled(string username, string reason) =>
            UserExpelled?.Invoke(username, reason);

        private void OnLobbyClosed(string reason) =>
            LobbyClosed?.Invoke(reason);

        public void Dispose()
        {
            if (isDisposed) return;

            if (callback != null)
            {
                callback.MessageReceived -= OnMessageReceived;
                callback.SystemNotificationReceived -= OnSystemNotificationReceived;
                callback.UserListUpdated -= OnUserListUpdated;
                callback.UserBanned -= OnUserBanned;
                callback.UserExpelled -= OnUserExpelled;
                callback.LobbyClosed -= OnLobbyClosed;
            }

            lock (clientLock)
            {
                try
                {
                    if (client?.State == CommunicationState.Opened)
                        client.Close();
                    else
                        client?.Abort();
                }
                catch { client?.Abort(); }
            }

            isDisposed = true;
        }
    }

}
