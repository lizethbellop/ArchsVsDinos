using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.Logging;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.RegisterService;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using ArchsVsDinosClient.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UserAccountDTO = ArchsVsDinosClient.DTO.UserAccountDTO;

namespace ArchsVsDinosClient.ViewModels
{
    public class RegisterViewModel
    {
        private IRegisterServiceClient registerService;
        private readonly IMessageService messageService;
        private readonly ILogger logger;

        public string Name { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Nickname { get; set; }

        public event EventHandler RequestClose;

        public RegisterViewModel()
        {
            logger = new Logger(typeof(RegisterViewModel));
            messageService = new MessageService();

            CreateRegisterService();
        }

        private void CreateRegisterService()
        {
            registerService?.Dispose();

            registerService = new RegisterServiceClient();
            registerService.ConnectionError += OnConnectionError;
        }

        public async Task RegisterAsync()
        {
            if (!ValidateInputs(Name, Username, Email, Password, Nickname))
                return;

            try
            {
                bool sent = await registerService.SendEmailRegisterAsync(Email);

                if (!sent)
                {
                    messageService.ShowMessage(Lang.Register_SentErrorCode);
                    return;
                }

                var codeWindow = new ConfirmCode
                {
                    Owner = Application.Current.Windows
                        .OfType<Window>()
                        .FirstOrDefault(w => w.IsActive)
                };

                codeWindow.ShowDialog();

                if (codeWindow.IsCancelled)
                {
                    messageService.ShowMessage(Lang.Register_CancelledRegistration);
                    return;
                }

                var user = new RegisterService.UserAccountDTO
                {
                    Name = Name,
                    Username = Username,
                    Email = Email,
                    Password = Password,
                    Nickname = Nickname
                };

                var response = await registerService.RegisterUserAsync(
                    user,
                    codeWindow.EnteredCode
                );

                if (response.Success)
                {
                    messageService.ShowMessage(Lang.Register_CorrectRegister);
                    RequestClose?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    HandleRegisterError(response.ResultCode);
                }
            }
            catch (FaultException<string> ex)
            {
                if (Enum.TryParse<RegisterResultCode>(ex.Detail, out var code))
                {
                    HandleRegisterError(code);
                }
                else
                {
                    RecoverFromConnectionError(Lang.GlobalServerError);
                }
            }
            catch (TimeoutException)
            {
                RecoverFromConnectionError(Lang.GlobalServerTimeout);
            }
            catch (CommunicationException ex)
            {
                if (ex.InnerException is SocketException)
                {
                    messageService.ShowMessage(
                        Lang.Register_NoInternetCannotSendCode
                    );
                }
                else
                {
                    RecoverFromConnectionError(Lang.GlobalServerUnavailable);
                }
            }

            catch (Exception ex)
            {
                logger.LogError("Unexpected register error", ex);
                RecoverFromConnectionError(Lang.GlobalServerUnavailable);
            }

        }

        private void RecoverFromConnectionError(string message)
        {
            CreateRegisterService();
            messageService.ShowMessage(message);
        }

        private void OnConnectionError(string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                messageService.ShowMessage($"{title}: {message}");
            });
        }

        private bool ValidateInputs(string name, string username, string email, string password, string nickname)
        {
            if (ValidationHelper.IsEmpty(name) ||
                ValidationHelper.IsEmpty(username) ||
                ValidationHelper.IsEmpty(email) ||
                ValidationHelper.IsEmpty(password) ||
                ValidationHelper.IsEmpty(nickname))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return false;
            }

            if (!ValidationHelper.IsAValidEmail(email))
            {
                messageService.ShowMessage(Lang.Register_InvalidEmail);
                return false;
            }

            if (!ValidationHelper.HasPasswordAllCharacters(password) ||
                !ValidationHelper.MinLengthPassword(password))
            {
                messageService.ShowMessage(Lang.Register_InvalidPassword);
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

                case RegisterResultCode.Register_DatabaseError:
                    messageService.ShowMessage(Lang.GlobalDatabaseError);
                    break;

                case RegisterResultCode.Register_UnexpectedError:
                    messageService.ShowMessage(Lang.GlobalServerError);
                    break;

                default:
                    messageService.ShowMessage(Lang.GlobalServerError);
                    break;
            }
        }

    }

}
