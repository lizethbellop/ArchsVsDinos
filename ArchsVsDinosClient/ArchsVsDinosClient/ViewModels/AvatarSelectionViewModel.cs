using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.ProfileManagerService;
using ArchsVsDinosClient.Properties.Langs;
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
        private readonly IProfileServiceClient profileService;
        private readonly IMessageService messageService;

        public string SelectedAvatarPath { get; private set; }
        private readonly Dictionary<int, string> avatarPaths;

        public event EventHandler RequestClose;

        public AvatarSelectionViewModel(IProfileServiceClient profileService, IMessageService messageService)
        {
            this.profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            this.messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            this.profileService.ConnectionError += OnConnectionError;

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
            if (avatarPaths.ContainsKey(avatarId))
            {
                SelectedAvatarPath = avatarPaths[avatarId];
            }
        }

        public async Task SaveSelectedAvatar()
        {
            if (!HasSelectedAvatar())
            {
                messageService.ShowMessage(Lang.Avatar_NoSelection);
                return;
            }

            try
            {
                string currentUsername = UserSession.Instance.CurrentUser.Username;

                UpdateResponse response = await profileService.ChangeProfilePictureAsync(
                    currentUsername,
                    SelectedAvatarPath
                );

                if (response == null || !response.Success)
                {
                    if (response != null)
                    {
                        string message = UpdateResultCodeHelper.GetMessage(response.ResultCode);
                        messageService.ShowMessage(message);
                    }
                    return;
                }

                string successMessage = UpdateResultCodeHelper.GetMessage(response.ResultCode);
                messageService.ShowMessage(successMessage);

                UserSession.Instance.CurrentPlayer.ProfilePicture = SelectedAvatarPath;

                UserProfileObserver.Instance.NotifyProfileUpdated();

                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                messageService.ShowMessage($"{Lang.Avatar_UnexpectedError}: {ex.Message}");
            }
        }

        private bool HasSelectedAvatar()
        {
            return !string.IsNullOrEmpty(SelectedAvatarPath);
        }

        private void OnConnectionError(string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                messageService.ShowMessage($"{title}: {message}");
            });
        }
    }
}
