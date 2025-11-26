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

        // ✅ AGREGAR ESTOS EVENTOS
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

            // ✅ SUSCRIBIRSE A LOS NUEVOS EVENTOS
            callback.UserBanned += OnUserBanned;
            callback.UserExpelled += OnUserExpelled;
            callback.LobbyClosed += OnLobbyClosed;

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
                async () => await Task.Run(() =>
                {
                    var request = new ChatConnectionRequest
                    {
                        Username = username,
                        Context = 0,
                        MatchCode = null
                    };
                    client.Connect(request);
                }),
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

        // ✅ AGREGAR ESTOS MÉTODOS PRIVADOS
        private void OnUserBanned(string username, int strikes)
        {
            UserBanned?.Invoke(username, strikes);
        }

        private void OnUserExpelled(string username, string reason)
        {
            UserExpelled?.Invoke(username, reason);
        }

        private void OnLobbyClosed(string reason)
        {
            LobbyClosed?.Invoke(reason);
        }

        public void Dispose()
        {
            if (isDisposed) return;

            if (callback != null)
            {
                callback.MessageReceived -= OnMessageReceived;
                callback.SystemNotificationReceived -= OnSystemNotificationReceived;
                callback.UserListUpdated -= OnUserListUpdated;

                // ✅ DESUSCRIBIRSE DE LOS NUEVOS EVENTOS
                callback.UserBanned -= OnUserBanned;
                callback.UserExpelled -= OnUserExpelled;
                callback.LobbyClosed -= OnLobbyClosed;
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
