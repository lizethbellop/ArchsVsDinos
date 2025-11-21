using ArchsVsDinosClient.AuthenticationService;
using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
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
using ServiceDTO = ArchsVsDinosClient.AuthenticationService;


namespace ArchsVsDinosClient.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly IAuthenticationServiceClient authenticationService;
        private readonly IMessageService messageService;
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

            if (response == null || !response.Success)
            {
                string message = LoginResultCodeHelper.GetMessage(response?.ResultCode ?? LoginResultCode.Authentication_UnexpectedError);
                messageService.ShowMessage(message);
                return;
            }

            string successMessage = LoginResultCodeHelper.GetMessage(response.ResultCode);
            messageService.ShowMessage(successMessage);

            ClientDTO.UserDTO user = response.UserSession.ToUserDTO();
            ClientDTO.PlayerDTO player = response.AssociatedPlayer.ToPlayerDTO();
            UserSession.Instance.Login(user, player);
            RequestClose?.Invoke(this, EventArgs.Empty);

        }

        private void OnConnectionError(string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                messageService.ShowMessage($"{title}: {message}");
            });
        }

        private bool ValidateInputs(string username, string password)
        {
            if (ValidationHelper.IsEmpty(username) || ValidationHelper.IsWhiteSpace(username) || (ValidationHelper.IsEmpty(password) || ValidationHelper.IsWhiteSpace(password)))
            {
                return false;
            }

            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
