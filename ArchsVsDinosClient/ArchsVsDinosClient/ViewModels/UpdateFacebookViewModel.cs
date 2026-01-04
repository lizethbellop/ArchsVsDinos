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
    public class UpdateFacebookViewModel
    {
        private IProfileServiceClient profileService;
        private readonly IMessageService messageService;
        private readonly ProfileServiceClientResetHelper resetHelper;

        public string NewFacebookLink { get; set; }
        public event EventHandler RequestClose;

        public UpdateFacebookViewModel(
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

        public async Task SaveFacebookLink()
        {
            if (!SocialMediaValidator.IsValidFacebookLink(NewFacebookLink))
            {
                messageService.ShowMessage(
                    SocialMediaValidator.GetValidationErrorMessage(SocialMediaPlatform.Facebook)
                );
                return;
            }

            string currentUsername = UserSession.Instance.CurrentUser.Username;

            try
            {
                var result = await resetHelper.ExecuteAsync(
                    profileService,
                    client => client.UpdateFacebookAsync(currentUsername, NewFacebookLink)
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

            UserSession.Instance.CurrentPlayer.Facebook = NewFacebookLink;
            UserProfileObserver.Instance.NotifyProfileUpdated();
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }

}
