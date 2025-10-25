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
    /// <summary>
    /// Lógica de interacción para Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        private void Btn_Register(object sender, RoutedEventArgs e)
        {
            new Register().ShowDialog();

        }

        private void Btn_Login(object sender, RoutedEventArgs e)
        {

            string username = TxtB_Username.Text;
            string password = Pb_Password.Password;


            if (!ValidateInputs(username, password)) 
            {
                MessageBox.Show(Lang.Global_EmptyField);
                return;
            } 
                
            try
            {
                AuthenticationService.AuthenticationManagerClient authenticationClient = new AuthenticationService.AuthenticationManagerClient();
                AuthenticationService.LoginResponse response = authenticationClient.Login(username, password);
                
            
                if (response.Success)
                {
                    UserDTO user = response.UserSession.ToUserDTO();
                    PlayerDTO player = response.AssociatedPlayer.ToPlayerDTO();

                    UserSession.Instance.Login(user, player);
                    
                    new MainWindow().ShowDialog();
                    this.Close();
                }
                else
                {
                    MessageBox.Show(Lang.Login_IncorrectCredentials);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Lang.Global_ServerError);
            }

        }

        private void Btn_PlayAsGuest(object sender, RoutedEventArgs e)
        {
            new MainWindow().ShowDialog();
            this.Close();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Cb_Language.SelectedIndex == 0)
            {
                Properties.Settings.Default.languageCode = "es-MX";

            }
            else
            {
                Properties.Settings.Default.languageCode = "en-US";
            }
            Properties.Settings.Default.Save();
        }

        private void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().ShowDialog();
        }


        private bool ValidateInputs(string username, string password)
        {
            if (ValidationHelper.isEmpty(username) || ValidationHelper.isWhiteSpace(username) || (ValidationHelper.isEmpty(password) || ValidationHelper.isWhiteSpace(password)))
            {
                return false;
            }

            return true;
        }

    }
}
