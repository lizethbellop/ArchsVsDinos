using ArchsVsDinosClient.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Utils
{
    public class ServiceOperationHelper
    {
        private readonly IMessageService messageService;

        public ServiceOperationHelper(IMessageService messageService)
        {
            this.messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        }

        public async Task<TResponse> ExecuteServiceOperationAsync<TResponse>(
            IServiceClient serviceClient,
            Func<Task<TResponse>> operation)
            where TResponse : class
        {
            TResponse response = await operation();

            if (!serviceClient.IsServerAvailable)
            {
                messageService.ShowMessage(
                    serviceClient.LastErrorTitle + "\n" +
                    serviceClient.LastErrorMessage
                );
                return null;
            }

            return response;
        }
    }
}
