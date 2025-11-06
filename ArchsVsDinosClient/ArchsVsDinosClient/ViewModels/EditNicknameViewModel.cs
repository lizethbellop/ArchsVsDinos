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
        private readonly IProfileServiceClient profileService;
        private readonly IMessageService messageService;
        public string NewNickname { get; set; }
        public event EventHandler RequestClose;

        public EditNicknameViewModel()
        {
            profileService = new ProfileServiceClient();
            messageService = new MessageService();
        }

        public async Task SaveEditNickname()
        {
            if (ValidateInputs(NewNickname))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return;
            }

            try
            {
                string currentUsername = UserSession.Instance.currentUser.username;
                UpdateResponse response = await profileService.UpdateNicknameAsync(currentUsername, NewNickname);

                string message = UpdateResultCodeHelper.GetMessage(response.ResultCode);
                messageService.ShowMessage(message);

                if (response.Success)
                {
                    UserSession.Instance.currentUser.nickname = NewNickname;
                    UserProfileObserver.Instance.NotifyProfileUpdated();
                    RequestClose?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (TimeoutException ex)
            {
                MessageBox.Show(Lang.GlobalServerError);
            }
            catch (CommunicationException ex)
            {
                // TODO: Logger cliente
            }
            catch (InvalidOperationException ex)
            {

            }
            catch (Exception ex)
            {

            }
        }

        private bool ValidateInputs(string nickname)
        {
            if (ValidationHelper.IsEmpty(nickname) || ValidationHelper.IsWhiteSpace(nickname))
            {
                return false;
            }

            return true;
        }
    }
}
