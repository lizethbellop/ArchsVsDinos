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
        private readonly FriendManagerClient client;
        private readonly WcfConnectionGuardian guardian;
        private bool isDisposed;

        public event Action<string, string> ConnectionError;

        public FriendServiceClient()
        {
            client = new FriendManagerClient();
            guardian = new WcfConnectionGuardian(
                onError: (title, msg) => ConnectionError?.Invoke(title, msg),
                logger: new Logger()
            );
            guardian.MonitorClientState(client);
        }

        public async Task<FriendResponse> RemoveFriendAsync(string username, string friendUsername)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.RemoveFriend(username, friendUsername)),
                defaultValue: new FriendResponse { Success = false },
                operationName: "eliminar amigo"
            );
        }

        public async Task<FriendListResponse> GetFriendsAsync(string username)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.GetFriends(username)),
                defaultValue: new FriendListResponse { Success = false, Friends = new string[0] },
                operationName: "obtener lista de amigos"
            );
        }

        public async Task<FriendCheckResponse> AreFriendsAsync(string username, string friendUsername)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.AreFriends(username, friendUsername)),
                defaultValue: new FriendCheckResponse { Success = false, AreFriends = false },
                operationName: "verificar amistad"
            );
        }

        public void Dispose()
        {
            if (isDisposed) return;

            try
            {
                if (client?.State == CommunicationState.Opened)
                    client.Close();
                else if (client?.State == CommunicationState.Faulted)
                    client.Abort();
            }
            catch
            {
                client?.Abort();
            }

            isDisposed = true;
        }
    }
}
