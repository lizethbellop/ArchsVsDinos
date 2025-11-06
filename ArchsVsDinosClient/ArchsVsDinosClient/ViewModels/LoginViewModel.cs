using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ArchsVsDinosClient.DTO;
using System.ServiceModel;

namespace ArchsVsDinosClient.ViewModels
{
    public class LoginViewModel
    {
        private readonly IAuthenticationServiceClient authenticationService;
        private readonly IMessageService messageService;
        public string Username { get; set; }
        public string Password { get; set; }

        public event EventHandler RequestClose;

        public LoginViewModel()
        {
            authenticationService = new AuthenticationServiceClient();
            messageService = new MessageService();
        }

        public async Task LoginAsync()
        {
            if (!ValidateInputs(Username, Password))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return;
            }

            try
            {
                var response = await authenticationService.LoginAsync(Username, Password);

                string message = LoginResultCodeHelper.GetMessage(response.ResultCode);
                messageService.ShowMessage(message);

                if (response.Success)
                {
                    UserDTO user = response.UserSession.ToUserDTO();
                    PlayerDTO player = response.AssociatedPlayer.ToPlayerDTO();
                    UserSession.Instance.Login(user, player);

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
            catch(Exception ex)
            {

            }
        }

        private bool ValidateInputs(string username, string password)
        {
            if (ValidationHelper.IsEmpty(username) || ValidationHelper.IsWhiteSpace(username) || (ValidationHelper.IsEmpty(password) || ValidationHelper.IsWhiteSpace(password)))
            {
                return false;
            }

            return true;
        }
    }
}
