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

        public string NewXLink { get; set; }
        public event EventHandler RequestClose;

        public UpdateXViewModel(IProfileServiceClient profileService, IMessageService messageService)
        {
            this.profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            this.messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

            this.profileService.ConnectionError += OnConnectionError;
        }

        public async Task SaveXLink()
        {
            if (!IsValidXLink(NewXLink))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return;
            }

            string currentUsername = UserSession.Instance.CurrentUser.Username;
            UpdateResponse response = await profileService.UpdateXAsync(currentUsername, NewXLink);

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
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void OnConnectionError(string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                messageService.ShowMessage($"{title}: {message}");
            });
        }

        private static bool IsValidXLink(string xLink)
        {
            return !string.IsNullOrWhiteSpace(xLink);
        }
    }
}
