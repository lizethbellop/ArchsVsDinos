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
    public class UpdateXViewModel
    {
        private IProfileServiceClient profileService;
        private readonly IMessageService messageService;
        private readonly ProfileServiceClientResetHelper resetHelper;

        public string NewXLink { get; set; }
        public event EventHandler RequestClose;

        public UpdateXViewModel(
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

        public async Task SaveXLink()
        {
            if (!SocialMediaValidator.IsValidXLink(NewXLink))
            {
                messageService.ShowMessage(
                    SocialMediaValidator.GetValidationErrorMessage(SocialMediaPlatform.X)
                );
                return;
            }

            string currentUsername = UserSession.Instance.CurrentUser.Username;

            try
            {
                var result = await resetHelper.ExecuteAsync(
                    profileService,
                    client => client.UpdateXAsync(currentUsername, NewXLink)
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

            UserSession.Instance.CurrentPlayer.X = NewXLink;
            UserProfileObserver.Instance.NotifyProfileUpdated();
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }

}
