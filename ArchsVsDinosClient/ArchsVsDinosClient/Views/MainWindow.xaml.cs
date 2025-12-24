using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Utils;
using ArchsVsDinosClient.ViewModels;
using ArchsVsDinosClient.Views;
using ArchsVsDinosClient.Views.LobbyViews;
using ArchsVsDinosClient.Views.MatchViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ArchsVsDinosClient
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ConfigureForTypeSession();
            //MusicPlayer.Instance.PlayBackgroundMusic(MusicTracks.Main);
        }

        private void ConfigureForTypeSession()
        {
            if (UserSession.Instance.IsGuest)
            {
                Btn_Creatematch.Visibility = Visibility.Collapsed;
                Btn_Friends.Visibility = Visibility.Collapsed;
                Btn_Account.Visibility = Visibility.Collapsed;
            }
        }

        private void Click_BtnLogOut(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            var login = new Login();
            login.Show();
            this.Close();
        }

        private void Click_BtnCreateMatch(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            var lobby = new Lobby();
            lobby.Show();
            this.Hide();
        }


        private void Click_BtnJoinToMatch(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            var joinCode = new JoinCode();
            joinCode.Show();
        }

        private void Click_BtnHowToPlay(object sender, RoutedEventArgs e)
        {
        }

        private void Click_BtnAccount(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new Account().ShowDialog();
        }

        private void Click_BtnFriends(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            string username = UserSession.Instance.CurrentUser.Username;
            new FriendsMainMenu(username).ShowDialog();
        }

        private void Click_BtnSettings(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new Settings().ShowDialog();
        }



    }
}
