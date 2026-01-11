using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Utils;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient.Views
{
    public class BaseSessionWindow : Window
    {
        private bool isReadyToClose = false;
        protected Func<Task> ExtraCleanupAction { get; set; }

        public bool IsNavigating { get; set; } = false;

        public bool ForceLogoutOnClose { get; set; } = false;

        protected override async void OnClosing(CancelEventArgs e)
        {
            if (isReadyToClose || UserSession.Instance.IsGuest || UserSession.Instance.CurrentUser == null)
            {
                base.OnClosing(e);
                return;
            }

            if (IsNavigating && !ForceLogoutOnClose)
            {
                base.OnClosing(e);
                return;
            }

            e.Cancel = true;
            this.Opacity = 0;
            this.ShowInTaskbar = false;

            try
            {
                if (ExtraCleanupAction != null)
                {
                    await ExtraCleanupAction.Invoke();
                }

                string username = UserSession.Instance.GetUsername();
                if (!string.IsNullOrEmpty(username))
                {
                    using (var client = new AuthenticationServiceClient())
                    {
                        await client.LogoutAsync(username);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally
            {
                UserSession.Instance.Logout();
                MusicPlayer.Instance.StopBackgroundMusic();

                isReadyToClose = true;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.Close();
                    if (!IsNavigating)
                    {
                        Application.Current.Shutdown();
                    }
                }));
            }
        }
    }
}