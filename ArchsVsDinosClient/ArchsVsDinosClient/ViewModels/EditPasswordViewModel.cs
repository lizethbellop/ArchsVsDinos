using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.ProfileManagerService;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient.ViewModels
{
    public class EditPasswordViewModel
    {
        private readonly IProfileServiceClient profileService;
        private readonly IMessageService messageService;

        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public event EventHandler RequestClose;

        public EditPasswordViewModel(IProfileServiceClient profileService, IMessageService messageService)
        {
            this.profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            this.messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        }

        public async Task SaveEditPassword()
        {
            if (!AreValidPasswords(CurrentPassword, NewPassword))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return;
            }

            try
            {
                string currentUsername = UserSession.Instance.CurrentUser.Username;
                UpdateResponse response = await profileService.ChangePassworsAsync(currentUsername, CurrentPassword, NewPassword);

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

        private static bool AreValidPasswords(string currentPassword, string newPassword)
        {
            return !string.IsNullOrWhiteSpace(currentPassword) && !string.IsNullOrWhiteSpace(newPassword);
        }
    }
}
