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

            TxtUsername.Text = user.username;
            TxtNickname.Text = user.nickname;
            TxtEmail.Text = user.email;
            TxtName.Text = user.name;
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

        private void Btn_Cancel(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            this.Close();   
        }

        private void Btn_PersonalStatistics(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new PersonalStatistics().ShowDialog();
        }

        private void Btn_EditPassword(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new EditPassword().ShowDialog();
        }

        private void Btn_EditUsername(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new EditUsername().ShowDialog();
        }

        private void Btn_EditNickname(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new EditAccountViews.EditNickname().ShowDialog();
        }

    }
}
