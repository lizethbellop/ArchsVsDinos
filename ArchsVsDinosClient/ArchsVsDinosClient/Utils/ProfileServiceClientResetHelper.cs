using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Utils
{
    public class ProfileServiceClientResetHelper
    {
        private readonly Func<IProfileServiceClient> clientFactory;
        private readonly IMessageService messageService;

        public ProfileServiceClientResetHelper(
            Func<IProfileServiceClient> clientFactory,
            IMessageService messageService)
        {
            this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            this.messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        }

        public async Task<(TResult Result, IProfileServiceClient Client)> ExecuteAsync<TResult>(
            IProfileServiceClient client,
            Func<IProfileServiceClient, Task<TResult>> action)
        {
            try
            {
                TResult result = await action(client);
                return (result, client);
            }
            catch (CommunicationException)
            {
                return (default, ResetClient(client));
            }
            catch (ObjectDisposedException)
            {
                return (default, ResetClient(client));
            }
        }

        private IProfileServiceClient ResetClient(IProfileServiceClient oldClient)
        {
            try
            {
                oldClient?.Dispose();
            }
            catch { }

            messageService.ShowMessage(Lang.GlobalNoConnectionToServer);

            return clientFactory();
        }
    }


}
