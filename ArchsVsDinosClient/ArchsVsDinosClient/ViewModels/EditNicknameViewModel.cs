using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.ProfileManagerService;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient.ViewModels
{
    public class EditNicknameViewModel
    {
        private IProfileServiceClient profileService;
        private readonly IMessageService messageService;
        private readonly ProfileServiceClientResetHelper resetHelper;

        public string NewNickname { get; set; }
        public event EventHandler RequestClose;

        public EditNicknameViewModel(IProfileServiceClient profileService, IMessageService messageService)
        {
            this.profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            this.messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

            this.resetHelper = new ProfileServiceClientResetHelper(
                () => new ProfileServiceClient(),
                messageService
            );
        }

        public async Task SaveEditNickname()
        {
            if (!IsValidNickname(NewNickname))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return;
            }

            string currentUsername = UserSession.Instance.CurrentUser.Username;

            var result = await resetHelper.ExecuteAsync(
                profileService,
                client => client.UpdateNicknameAsync(currentUsername, NewNickname)
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
            UserSession.Instance.CurrentUser.Nickname = NewNickname;
            UserProfileObserver.Instance.NotifyProfileUpdated();
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private static bool IsValidNickname(string nickname)
        {
            return !string.IsNullOrWhiteSpace(nickname);
        }
    }

}
