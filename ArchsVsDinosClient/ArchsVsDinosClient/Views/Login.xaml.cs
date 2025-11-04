using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.Utils;
using AuthenticationService = ArchsVsDinosClient.AuthenticationService;

namespace ArchsVsDinosClient
{
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        private void Click_BtnRegister(object sender, RoutedEventArgs e)
        {
            new Register().ShowDialog();

        }

        private void Click_BtnLogin(object sender, RoutedEventArgs e)
        {

            string username = TxtB_Username.Text;
            string password = PB_Password.Password;


            if (!ValidateInputs(username, password)) 
            {
                MessageBox.Show(Lang.GlobalEmptyField);
                return;
            }

            try
            {
                AuthenticationService.AuthenticationManagerClient authenticationClient = new AuthenticationService.AuthenticationManagerClient();
                AuthenticationService.LoginResponse response = authenticationClient.Login(username, password);

                string message = LoginResultCodeHelper.GetMessage(response.ResultCode);
                MessageBox.Show(message);

                if (response.Success)
                {
                    UserDTO user = response.UserSession.ToUserDTO();
                    PlayerDTO player = response.AssociatedPlayer.ToPlayerDTO();
                    UserSession.Instance.Login(user, player);

                    new MainWindow().ShowDialog();
                    this.Close();
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(Lang.GlobalServerError);
            }

        }

        private void Click_BtnPlayAsGuest(object sender, RoutedEventArgs e)
        {
            UserSession.Instance.LoginAsGuest();
            new MainWindow().ShowDialog();
            this.Close();
        }
        
        private void SelectionChanged_CbLanguage(object sender, SelectionChangedEventArgs e)
        {
            if (CB_Language.SelectedIndex == 0)
            {
                Properties.Settings.Default.languageCode = "es-MX";

            }
            else
            {
                Properties.Settings.Default.languageCode = "en-US";
            }
            Properties.Settings.Default.Save();
        }

        private void Click_BtnChangeLanguage(object sender, RoutedEventArgs e)
        {
            new MainWindow().ShowDialog();
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
