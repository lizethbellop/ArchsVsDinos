using ArchsVsDinosClient.AuthenticationService;
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
    public class AuthenticationServiceClient : IAuthenticationServiceClient
    {
        private readonly AuthenticationManagerClient client;
        private readonly WcfConnectionGuardian guardian; 
        private bool isDisposed;

        public event Action<string, string> ConnectionError;

        public AuthenticationServiceClient()
        {
            client = new AuthenticationManagerClient();

            guardian = new WcfConnectionGuardian(
                onError: (title, msg) => ConnectionError?.Invoke(title, msg),
                logger: new Logger()
            );
            guardian.MonitorClientState(client);
        }

        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            return await guardian.ExecuteAsync(
                async () => await Task.Run(() => client.Login(username, password)),
                defaultValue: new LoginResponse { Success = false }
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
            catch { client?.Abort(); }
            isDisposed = true;
        }
    }
}
