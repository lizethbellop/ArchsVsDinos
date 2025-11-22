using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Utils;
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

namespace ArchsVsDinosClient.Views
{
    /// <summary>
    /// Lógica de interacción para Account.xaml
    /// </summary>
    public partial class Account : Window
    {
        public Account()
        {
            InitializeComponent();
            LoadUserData();
            UserProfileObserver.Instance.OnProfileUpdated += RefreshUserData;
        }

        private void LoadUserData()
        {
            var user = UserSession.Instance.CurrentUser;

            TxtUsername.Text = user.Username;
            TxtNickname.Text = user.Nickname;
            TxtEmail.Text = user.Email;
            TxtName.Text = user.Name;
            MusicPlayer.Instance.PlayBackgroundMusic(MusicTracks.Main);
        }

        private void RefreshUserData()
        {
            LoadUserData();
        }

        protected override void OnClosed(EventArgs e)
        {
            UserProfileObserver.Instance.OnProfileUpdated -= RefreshUserData;
            base.OnClosed(e);
        }

        private void Click_BtnCancel(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            this.Close();   
        }

        private void Click_BtnPersonalStatistics(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new PersonalStatistics().ShowDialog();
        }

        private void Click_BtnEditPassword(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new EditPassword().ShowDialog();
        }

        private void Click_BtnEditUsername(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new EditUsername().ShowDialog();
        }

        private void Click_BtnEditNickname(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new EditAccountViews.EditNickname().ShowDialog();
        }

        private void Click_BtnEditAvatar(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new AvatarSelection().ShowDialog();
        }

    }
}
