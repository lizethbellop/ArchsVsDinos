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
    public class UpdateTikTokViewModel
    {
        private readonly IProfileServiceClient profileService;
        private readonly IMessageService messageService;
        private readonly ServiceOperationHelper serviceHelper;

        public string NewTikTokLink { get; set; }
        public event EventHandler RequestClose;

        public UpdateTikTokViewModel(IProfileServiceClient profileService, IMessageService messageService)
        {
            this.profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            this.messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            this.serviceHelper = new ServiceOperationHelper(messageService);
        }

        public async Task SaveTikTokLink()
        {
            if (!SocialMediaValidator.IsValidTikTokLink(NewTikTokLink))
            {
                messageService.ShowMessage(SocialMediaValidator.GetValidationErrorMessage(SocialMediaPlatform.TikTok));
                return;
            }

            string currentUsername = UserSession.Instance.CurrentUser.Username;
            UpdateResponse response = await serviceHelper.ExecuteServiceOperationAsync(
                profileService,
                () => profileService.UpdateTikTokAsync(currentUsername, NewTikTokLink)
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
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

    }
}
