using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.RegisterService;
using ArchsVsDinosClient.Utils;
using ArchsVsDinosClient.Views;
using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UserAccountDTO = ArchsVsDinosClient.DTO.UserAccountDTO;
using System.ServiceModel;

namespace ArchsVsDinosClient.ViewModels
{
    public class RegisterViewModel
    {

        private readonly IRegisterManager registerService;
        private readonly IMessageService messageService;

        public string Name { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Nickname { get; set; }


        public event EventHandler RequestClose;

        public RegisterViewModel()
        {
            registerService = new RegisterManagerClient();
            messageService = new MessageService();
        }

        public async Task RegisterAsync()
        {

            if (!ValidateInputs(Name, Username, Email, Password, Nickname))
            {
                return;
            }

            try
            {

                bool sent = await Task.Run(() => registerService.SendEmailRegister(Email));
                
                if (!sent)
                {
                    MessageBox.Show(Lang.Register_SentErrorCode);
                    return;
                }

                var  codeWindow = new ConfirmCode
                {
                    Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive)
                };
                codeWindow.ShowDialog();

                if (codeWindow.IsCancelled)
                {
                    messageService.ShowMessage(Lang.Register_CancelledRegistration);
                    return;
                }

                string code = codeWindow.EnteredCode;

                var userAccount = new UserAccountDTO
                {
                    Name = Name,
                    Username = Username,
                    Email = Email,
                    Password = Password,
                    Nickname = Nickname
                };

                var serviceUserAccount = new RegisterService.UserAccountDTO
                {
                    Name = userAccount.Name,
                    Username = userAccount.Username,
                    Email = userAccount.Email,
                    Password = userAccount.Password,
                    Nickname = userAccount.Nickname
                };

                RegisterResponse registered = await Task.Run(() => registerService.RegisterUser(serviceUserAccount, code));

                if (registered.Success)
                {
                    messageService.ShowMessage(Lang.Register_CorrectRegister);
                    RequestClose?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    HandleRegisterError(registered.ResultCode);
                }
            }
            catch (TimeoutException ex)
            {
                MessageBox.Show(Lang.GlobalServerError);
            }

            catch (CommunicationException ex)
            {
                MessageBox.Show(Lang.GlobalServerError);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Lang.GlobalServerError);
            }

        }

        private bool ValidateInputs(string name, string username, string email, string password, string nickname)
        {
            if (ValidationHelper.IsEmpty(name) || ValidationHelper.IsEmpty(username) || ValidationHelper.IsEmpty(email) || ValidationHelper.IsEmpty(password) || ValidationHelper.IsEmpty(nickname))
            {
                MessageBox.Show(Lang.GlobalEmptyField);
                return false;
            }

            if (!ValidationHelper.IsAValidEmail(email))
            {
                MessageBox.Show(Lang.Register_InvalidEmail);
                return false;
            }

            if (!ValidationHelper.HasPasswordAllCharacters(password) || !ValidationHelper.MinLengthPassword(password))
            {
                MessageBox.Show(Lang.Register_InvalidPassword);
                return false;
            }


            return true;
        }

        private void HandleRegisterError(RegisterResultCode code)
        {
            switch (code)
            {
                case RegisterResultCode.Register_InvalidCode:
                    messageService.ShowMessage(Lang.Register_IncorrectCode);
                    break;
                case RegisterResultCode.Register_BothExists:
                    messageService.ShowMessage(Lang.Register_UsernameAndNicknameExists);
                    break;
                case RegisterResultCode.Register_UsernameExists:
                    messageService.ShowMessage(Lang.Register_UsernameAlreadyExists);
                    break;
                case RegisterResultCode.Register_NicknameExists:
                    messageService.ShowMessage(Lang.Register_NicknameAlreadyExists);
                    break;
                case RegisterResultCode.Register_UnexpectedError:
                    messageService.ShowMessage(Lang.GlobalServerError);
                    break;
            }
        }

    }
}
