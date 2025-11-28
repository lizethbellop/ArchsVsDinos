using ArchsVsDinosClient.AuthenticationService;
using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.Logging;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ClientDTO = ArchsVsDinosClient.DTO;
using Logger = ArchsVsDinosClient.Logging.Logger;
using ServiceDTO = ArchsVsDinosClient.AuthenticationService;


namespace ArchsVsDinosClient.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly IAuthenticationServiceClient authenticationService;
        private readonly IMessageService messageService;
        private readonly ILogger log;

        private string username;
        private string password;

        public string Username
        {
            get => username;
            set { username = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => password;
            set { password = value; OnPropertyChanged(); }
        }

        public event EventHandler RequestClose;
        public event PropertyChangedEventHandler PropertyChanged;

        public LoginViewModel()
        {
            log = new Logger(typeof(AuthenticationServiceClient));
            authenticationService = new AuthenticationServiceClient();
            messageService = new MessageService();

            authenticationService.ConnectionError += OnConnectionError;
        }

        public async Task LoginAsync()
        {
            if (!ValidateInputs(Username, Password))
            {
                messageService.ShowMessage(Lang.GlobalEmptyField);
                return;
            }

            var response = await authenticationService.LoginAsync(Username, Password);

            if (!authenticationService.IsServerAvailable)
            {
                messageService.ShowMessage(
                    authenticationService.LastErrorTitle + "\n" +
                    authenticationService.LastErrorMessage
                );
                return;
            }

            if (response == null || !response.Success)
            {
                string message = LoginResultCodeHelper.GetMessage(
                    response?.ResultCode ?? LoginResultCode.Authentication_UnexpectedError
                );
                messageService.ShowMessage(message);
                return;
            }

            string successMessage = LoginResultCodeHelper.GetMessage(response.ResultCode);
            messageService.ShowMessage(successMessage);

            var user = response.UserSession.ToUserDTO();
            var player = response.AssociatedPlayer.ToPlayerDTO();

            UserSession.Instance.Login(user, player);
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void OnConnectionError(string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                messageService.ShowMessage(title + ": " + message);
            });
        }

        private bool ValidateInputs(string username, string password)
        {
            return !(ValidationHelper.IsEmpty(username) ||
                     ValidationHelper.IsWhiteSpace(username) ||
                     ValidationHelper.IsEmpty(password) ||
                     ValidationHelper.IsWhiteSpace(password));
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


}
