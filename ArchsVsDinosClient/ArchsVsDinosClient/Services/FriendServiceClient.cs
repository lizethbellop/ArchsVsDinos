using ArchsVsDinosClient.FriendService;
using ArchsVsDinosClient.Logging;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services
{
    public class FriendServiceClient : IFriendServiceClient
    {
        private FriendManagerClient client;
        private readonly WcfConnectionGuardian guardian;
        private readonly object clientLock = new object();
        private bool isDisposed;

        public event Action<string, string> ConnectionError;

        public FriendServiceClient()
        {
            guardian = new WcfConnectionGuardian(
                onError: (title, msg) => ConnectionError?.Invoke(title, msg),
                logger: new Logger()
            );

            try
            {
                InitializeClient();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error inicializando FriendServiceClient: {ex.Message}");
            }
        }

        private void InitializeClient()
        {
            lock (clientLock)
            {
                client = new FriendManagerClient();
                guardian.MonitorClientState(client);
            }
        }

        private void RegenerateClient()
        {
            lock (clientLock)
            {
                try
                {
                    if (client?.State == CommunicationState.Faulted)
                        client.Abort();
                    else
                        client?.Close();
                }
                catch
                {
                    client?.Abort();
                }

                client = new FriendManagerClient();
                guardian.MonitorClientState(client);
            }
        }

        public async Task<FriendResponse> RemoveFriendAsync(string username, string friendUsername)
        {
            return await guardian.ExecuteWithThrowAsync(
                () => Task.FromResult(client.RemoveFriend(username, friendUsername)),
                operationName: "eliminar amigo"
            );
        }

        public async Task<FriendListResponse> GetFriendsAsync(string username)
        {
            return await guardian.ExecuteWithThrowAsync(
                () => Task.FromResult(client.GetFriends(username)),
                operationName: "obtener lista de amigos"
            );
        }

        public async Task<FriendCheckResponse> AreFriendsAsync(string username, string friendUsername)
        {
            return await guardian.ExecuteWithThrowAsync(
                () => Task.FromResult(client.AreFriends(username, friendUsername)),
                operationName: "verificar amistad"
            );
        }

        public void Dispose()
        {
            if (isDisposed) return;

            lock (clientLock)
            {
                try
                {
                    if (client?.State == CommunicationState.Opened)
                        client.Close();
                    else
                        client?.Abort();
                }
                catch
                {
                    client?.Abort();
                }
            }

            isDisposed = true;
        }
    }
}
