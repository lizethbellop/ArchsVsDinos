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
    public class EditUsernameViewModel
    {
        private IProfileServiceClient profileService;
        private readonly IMessageService messageService;
        private readonly ProfileServiceClientResetHelper resetHelper;

        public string NewUsername { get; set; }
        public event EventHandler RequestClose;

        public EditUsernameViewModel(
            IProfileServiceClient profileService,
            IMessageService messageService)
        {
            this.profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            this.messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

            resetHelper = new ProfileServiceClientResetHelper(
                () => new ProfileServiceClient(),
                messageService
            );
        }

        public async Task SaveEditUsername()
        {
            if (!IsValidUsername(NewUsername))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return;
            }

            string currentUsername = UserSession.Instance.CurrentUser.Username;

            try
            {
                var result = await resetHelper.ExecuteAsync(
                    profileService,
                    client => client.UpdateUsernameAsync(currentUsername, NewUsername)
                );

                profileService = result.Client;

                if (!result.Result.Success)
                {
                    messageService.ShowMessage(
                        UpdateResultCodeHelper.GetMessage(result.Result.ResultCode)
                    );
                    return;
                }

                HandleSuccess(result.Result);
            }
            catch
            {
                
            }
        }

        private void HandleSuccess(UpdateResponse response)
        {
            messageService.ShowMessage(
                UpdateResultCodeHelper.GetMessage(response.ResultCode)
            );

            UserSession.Instance.CurrentUser.Username = NewUsername;
            UserProfileObserver.Instance.NotifyProfileUpdated();

            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private static bool IsValidUsername(string username)
        {
            return !string.IsNullOrWhiteSpace(username);
        }
    }

}
