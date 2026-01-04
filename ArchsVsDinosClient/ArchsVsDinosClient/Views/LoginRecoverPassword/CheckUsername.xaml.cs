using ArchsVsDinosClient.AuthenticationService;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
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
using System.Windows.Threading;

namespace ArchsVsDinosClient.Views.LoginRecoverPassword
{
    public partial class CheckUsername : Window
    {

        private DispatcherTimer timer;
        private int timeRemaining;
        private DateTime cooldownEndTime;
        private IAuthenticationServiceClient authService;

        public CheckUsername()
        {
            InitializeComponent();
            authService = new AuthenticationServiceClient();

            InitializeTimer();

            Lb_Timer.Visibility = Visibility.Collapsed;

            TxtB_NewNickname.TextChanged += TxtB_NewNickname_TextChanged;
        }

        private void InitializeTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            TimeSpan timeRemaining = cooldownEndTime - DateTime.Now;

            if (timeRemaining.TotalSeconds > 0)
            {
                Lb_Timer.Content = string.Format($"{Lang.ChangeP_TimeRemaining} {0:D2}:{1:D2}",
                                                 timeRemaining.Minutes,
                                                 timeRemaining.Seconds);
            }
            else
            {
                timer.Stop();
                Lb_Timer.Visibility = Visibility.Collapsed;
            }
        }

        private void TxtB_NewNickname_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Lb_Timer.Visibility == Visibility.Visible)
            {
                timer.Stop();
                Lb_Timer.Visibility = Visibility.Collapsed;
            }
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
            string username = TxtB_NewNickname.Text.Trim();

            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show(Lang.ChangeP_EmptyUsername);
                return;
            }

            try
            {
                this.Cursor = System.Windows.Input.Cursors.Wait;

                var response = await authService.SendRecoveryCodeAsync(username);

                switch (response.Result)
                {
                    case PasswordRecoveryResult.PasswordRecovery_Success:
                        GoToEnterCode(username);
                        break;

                    case PasswordRecoveryResult.PasswordRecovery_CooldownActive:
                        cooldownEndTime = DateTime.Now.AddSeconds(response.RemainingSeconds);

                        TimeSpan initialDiff = cooldownEndTime - DateTime.Now;
                        Lb_Timer.Content = string.Format($"{Lang.ChangeP_TimeRemaining} {0:D2}:{1:D2}",
                                                         initialDiff.Minutes,
                                                         initialDiff.Seconds);
                        Lb_Timer.Visibility = Visibility.Visible;
                        timer.Start();

                        MessageBox.Show(Lang.ChangeP_EmailAlreadySended);
                        break;

                    case PasswordRecoveryResult.PasswordRecovery_UserNotFound:
                        MessageBox.Show(Lang.ChangeP_UserDoesntExist);
                        break;

                    case PasswordRecoveryResult.PasswordRecovery_DatabaseError:
                    case PasswordRecoveryResult.PasswordRecovery_ServerError:
                    case PasswordRecoveryResult.PasswordRecovery_ConnectionError:
                    case PasswordRecoveryResult.PasswordRecovery_UnexpectedError:
                        MessageBox.Show(Lang.GlobalServerError);
                        break;
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

        private void GoToEnterCode(string username)
        {
            var enterCodeWindow = new EnterCode(username);
            enterCodeWindow.Show();
            this.Close();
        }

        private void Click_BtnCancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
