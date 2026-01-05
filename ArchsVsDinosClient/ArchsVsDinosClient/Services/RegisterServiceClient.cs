using ArchsVsDinosClient.RegisterService;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using ArchsVsDinosClient.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services
{
    public class RegisterServiceClient : IRegisterServiceClient
    {
        private RegisterManagerClient client;
        private readonly WcfConnectionGuardian guardian;
        private readonly ILogger logger;

        public event Action<string, string> ConnectionError;

        public bool IsServerAvailable => guardian.IsServerAvailable;

        public RegisterServiceClient()
        {
            logger = new Logger(typeof(RegisterServiceClient));
            client = new RegisterManagerClient();
        
            guardian = new WcfConnectionGuardian(
                (title, msg) => ConnectionError?.Invoke(title, msg),
                logger
            );

            guardian.MonitorClientState(client);
        }

        public async Task<bool> SendEmailRegisterAsync(string email)
        {
            return await guardian.ExecuteWithThrowAsync(
                () => Task.FromResult(client.SendEmailRegister(email)),
                "SendEmailRegister"
            );
        }

        public async Task<RegisterResponse> RegisterUserAsync(UserAccountDTO user, string code)
        {
            return await guardian.ExecuteWithThrowAsync(
                () => Task.FromResult(client.RegisterUser(user, code)),
                "RegisterUser"
            );
        }

        public void Dispose()
        {
            try
            {
                if (client.State == CommunicationState.Faulted)
                    client.Abort();
                else
                    client.Close();
            }
            catch
            {
                client.Abort();
            }
        }
    }

}
