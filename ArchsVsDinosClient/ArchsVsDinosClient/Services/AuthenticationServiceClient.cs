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
        private readonly ILogger logger;
        private bool isDisposed;

        public event Action<string, string> ConnectionError;

        public bool IsServerAvailable => guardian.IsServerAvailable;
        public string LastErrorTitle => guardian.LastErrorTitle;
        public string LastErrorMessage => guardian.LastErrorMessage;

        public AuthenticationServiceClient()
        {
            logger = new Logger(typeof(AuthenticationServiceClient));

            client = new AuthenticationManagerClient();

            guardian = new WcfConnectionGuardian(
                onError: (title, msg) =>
                {
                    logger.LogError($"🔴 Guardian reportó error: {title} - {msg}");
                    ConnectionError?.Invoke(title, msg);
                },
                logger: logger
            );

            guardian.MonitorClientState(client);
        }

        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            return await guardian.ExecuteWithThrowAsync(
                () => Task.FromResult(client.Login(username, password)),
                operationName: "Login"
            );
        }

        public async Task LogoutAsync(string username)
        {
            await guardian.ExecuteWithThrowAsync<bool>(
                () =>
                {
                    client.Logout(username);
                    return Task.FromResult(true);
                },
                operationName: "Logout"
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
