using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.ProfileManagerService;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.ViewModels
{
    public class UpdateFacebookViewModel
    {
        private readonly IProfileServiceClient profileService;
        private readonly IMessageService messageService;

        public string NewFacebookLink { get; set; }
        public event EventHandler RequestClose;

        public UpdateFacebookViewModel(IProfileServiceClient profileService, IMessageService messageService)
        {
            this.profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            this.messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        }

        public async Task SaveFacebookLink()
        {
            if (!IsValidFacebookLink(NewFacebookLink))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return;
            }

            try
            {
                string currentUsername = UserSession.Instance.CurrentUser.Username;
                UpdateResponse response = await profileService.UpdateFacebookAsync(currentUsername, NewFacebookLink);

                string message = UpdateResultCodeHelper.GetMessage(response.ResultCode);
                messageService.ShowMessage(message);

                if (response.Success)
                {
                    RequestClose?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (TimeoutException ex)
            {
                messageService.ShowMessage(Lang.GlobalServerError);
                // TODO: Logger cliente
            }
            catch (CommunicationException ex)
            {
                // TODO: Logger cliente
            }
            catch (InvalidOperationException ex)
            {
                // TODO: Logger cliente
            }
            catch (Exception ex)
            {
                // TODO: Logger cliente
            }
        }

        private static bool IsValidFacebookLink(string facebookLink)
        {
            return !string.IsNullOrWhiteSpace(facebookLink);
        }
    }
}
