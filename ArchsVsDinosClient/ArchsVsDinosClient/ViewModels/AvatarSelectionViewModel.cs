using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.ProfileManagerService;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient.ViewModels
{
    public class AvatarSelectionViewModel
    {
        private IProfileServiceClient profileService;
        private readonly IMessageService messageService;
        private readonly ProfileServiceClientResetHelper resetHelper;

        public string SelectedAvatarPath { get; private set; }

        private readonly Dictionary<int, string> avatarPaths;

        public event EventHandler RequestClose;

        public AvatarSelectionViewModel(
            IProfileServiceClient profileService,
            IMessageService messageService)
        {
            this.profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            this.messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

            resetHelper = new ProfileServiceClientResetHelper(
                () => new ProfileServiceClient(),
                messageService
            );

            avatarPaths = new Dictionary<int, string>
        {
            { 1, "/Resources/Images/Avatars/default_avatar_01.png" },
            { 2, "/Resources/Images/Avatars/default_avatar_02.png" },
            { 3, "/Resources/Images/Avatars/default_avatar_03.png" },
            { 4, "/Resources/Images/Avatars/default_avatar_04.png" },
            { 5, "/Resources/Images/Avatars/default_avatar_05.png" }
        };
        }

        public void SelectAvatar(int avatarId)
        {
            if (avatarPaths.TryGetValue(avatarId, out var path))
            {
                SelectedAvatarPath = path;
            }
        }

        public async Task SaveSelectedAvatar()
        {
            if (string.IsNullOrEmpty(SelectedAvatarPath))
            {
                messageService.ShowMessage(Lang.Avatar_NoSelection);
                return;
            }

            string currentUsername = UserSession.Instance.CurrentUser.Username;

            try
            {
                var result = await resetHelper.ExecuteAsync(
                    profileService,
                    client => client.ChangeProfilePictureAsync(currentUsername, SelectedAvatarPath)
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

            UserSession.Instance.CurrentPlayer.ProfilePicture = SelectedAvatarPath;
            UserProfileObserver.Instance.NotifyProfileUpdated();

            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }

}
