using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;

namespace ArchsVsDinosClient.Views.LoginRecoverPassword
{
    public partial class NewPassword : Window
    {
        private string username;
        private IAuthenticationServiceClient authService;

        public NewPassword(string username)
        {
            InitializeComponent();
            this.username = username;
            authService = new AuthenticationServiceClient();

            Lb_WrongPassword.Visibility = Visibility.Collapsed;

            PB_NewNickname.PasswordChanged += PasswordChanged;
            PB_ConfirmNewNickname.PasswordChanged += PasswordChanged;
        }

        public NewPassword() : this("")
        {
        }

        private void ResetAuthenticationService()
        {
            if (authService != null)
            {
                authService.Dispose();
            }
            authService = new AuthenticationServiceClient();
        }

        private void PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (Lb_WrongPassword.Visibility == Visibility.Visible)
            {
                Lb_WrongPassword.Visibility = Visibility.Collapsed;
            }
        }

        private async void Click_BtnSave(object sender, RoutedEventArgs e)
        {
            string pass1 = PB_NewNickname.Password;
            string pass2 = PB_ConfirmNewNickname.Password;

            if (ValidationHelper.IsEmpty(pass1) || ValidationHelper.IsEmpty(pass2))
            {
                MessageBox.Show(Lang.GlobalEmptyField);
                return;
            }

            if (pass1 != pass2)
            {
                Lb_WrongPassword.Visibility = Visibility.Visible;
                return;
            }

            if (!ValidationHelper.HasPasswordAllCharacters(pass1))
            {
                MessageBox.Show(Lang.Register_InvalidPassword);
                return;
            }

            try
            {
                this.Cursor = System.Windows.Input.Cursors.Wait;

                bool result = await authService.UpdatePasswordAsync(username, pass1);

                if (result)
                {
                    MessageBox.Show(Lang.ChangeP_SuccessChangePassword); 
                    this.Close();
                }
                else
                {
                    MessageBox.Show(Lang.GlobalServerError);
                }
            }
            catch (TimeoutException)
            {
                ResetAuthenticationService();
                MessageBox.Show(Lang.WcfErrorTimeout);
            }
            catch (CommunicationException)
            {
                ResetAuthenticationService();
                MessageBox.Show(Lang.WcfErrorUnexpected);
            }
            catch (Exception)
            {
                ResetAuthenticationService();
                MessageBox.Show(Lang.GlobalServerError);
            }
            finally
            {
                this.Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        private void Click_BtnCancel(object sender, RoutedEventArgs e)
        {
            if (authService != null)
            {
                authService.Dispose();
            }
            this.Close();
        }
    }
}