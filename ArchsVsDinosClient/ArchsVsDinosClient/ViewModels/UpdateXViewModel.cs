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
using System.Windows;

namespace ArchsVsDinosClient.ViewModels
{
    public class UpdateXViewModel
    {
        private readonly IProfileServiceClient profileService;
        private readonly IMessageService messageService;
        private readonly ServiceOperationHelper serviceHelper;

        public string NewXLink { get; set; }
        public event EventHandler RequestClose;

        public UpdateXViewModel(IProfileServiceClient profileService, IMessageService messageService)
        {
            this.profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            this.messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            this.serviceHelper = new ServiceOperationHelper(messageService);
        }

        public async Task SaveXLink()
        {
            if (!SocialMediaValidator.IsValidXLink(NewXLink))
            {
                messageService.ShowMessage(SocialMediaValidator.GetValidationErrorMessage(SocialMediaPlatform.X));
                return;
            }

            string currentUsername = UserSession.Instance.CurrentUser.Username;
            UpdateResponse response = await serviceHelper.ExecuteServiceOperationAsync(
                profileService,
                () => profileService.UpdateXAsync(currentUsername, NewXLink)
            );

            if (response == null)
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
            UserSession.Instance.CurrentPlayer.X = NewXLink;
            UserProfileObserver.Instance.NotifyProfileUpdated();
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

    }
}
