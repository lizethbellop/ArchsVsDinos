using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System;
using System.ServiceModel;
using System.Windows;

namespace ArchsVsDinosClient.Views.LoginRecoverPassword
{
    public partial class EnterCode : Window
    {
        private string username;
        private IAuthenticationServiceClient authService; 

        public EnterCode(string username)
        {
            InitializeComponent();
            this.username = username;
            authService = new AuthenticationServiceClient();
        }

        public EnterCode() : this("")
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

        private async void Click_BtnSend(object sender, RoutedEventArgs e)
        {
            string code = TxtB_NewPassword.Text.Trim();

            if (ValidationHelper.IsEmpty(code) || code.Length != 5)
            {
                MessageBox.Show(Lang.Register_IncorrectCode);
                return;
            }

            try
            {
                this.Cursor = System.Windows.Input.Cursors.Wait;

                bool isValid = await authService.ValidateRecoveryCodeAsync(username, code);

                if (isValid)
                {
                    var newPassWindow = new NewPassword(username);
                    newPassWindow.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show(Lang.Register_IncorrectCode);
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