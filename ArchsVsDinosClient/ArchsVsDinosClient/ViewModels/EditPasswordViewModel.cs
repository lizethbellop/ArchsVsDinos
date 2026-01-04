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
        private IProfileServiceClient profileService;
        private readonly IMessageService messageService;
        private readonly ProfileServiceClientResetHelper resetHelper;

        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public event EventHandler RequestClose;

        public EditPasswordViewModel(IProfileServiceClient profileService, IMessageService messageService)
        {
            this.profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            this.messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

            this.resetHelper = new ProfileServiceClientResetHelper(
                () => new ProfileServiceClient(),
                messageService
            );
        }

        public async Task SaveEditPassword()
        {
            if (!AreValidPasswords(CurrentPassword, NewPassword))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return;
            }

            if (CurrentPassword == NewPassword)
            {
                messageService.ShowMessage(Lang.Profile_SamePasswordValue);
                return;
            }

            var validationResult = PasswordValidator.ValidatePassword(NewPassword);
            if (!validationResult.IsValid)
            {
                messageService.ShowMessage(validationResult.ErrorMessage);
                return;
            }

            string currentUsername = UserSession.Instance.CurrentUser.Username;

            var result = await resetHelper.ExecuteAsync(
                profileService,
                client => client.ChangePassworsAsync(currentUsername, CurrentPassword, NewPassword)
            );

            profileService = result.Client;

            UpdateResponse response = result.Result;
            if (response == null)
                return;

            if (!response.Success)
            {
                string message = UpdateResultCodeHelper.GetMessage(response.ResultCode);
                messageService.ShowMessage(message);
                return;
            }

            HandleSuccess(response);
        }

        private void HandleSuccess(UpdateResponse response)
        {
            string successMessage = UpdateResultCodeHelper.GetMessage(response.ResultCode);
            messageService.ShowMessage(successMessage);
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private static bool AreValidPasswords(string currentPassword, string newPassword)
        {
            return !string.IsNullOrWhiteSpace(currentPassword)
                && !string.IsNullOrWhiteSpace(newPassword);
        }
    }

}
