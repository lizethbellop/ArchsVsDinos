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
        private readonly IProfileServiceClient profileService;
        private readonly IMessageService messageService;
        private readonly ServiceOperationHelper serviceHelper;

        public string NewUsername { get; set; }
        public event EventHandler RequestClose;

        public EditUsernameViewModel(IProfileServiceClient profileService, IMessageService messageService)
        {
            this.profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            this.messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            this.serviceHelper = new ServiceOperationHelper(messageService);
        }

        public async Task SaveEditUsername()
        {
            if (!IsValidUsername(NewUsername))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return;
            }

            string currentUsername = UserSession.Instance.CurrentUser.Username;
            UpdateResponse response = await serviceHelper.ExecuteServiceOperationAsync(
                profileService,
                () => profileService.UpdateUsernameAsync(currentUsername, NewUsername)
            );

            if(response == null)
            {
                return;
            }

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
