using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.LobbyService;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Utils;
using ArchsVsDinosClient.ViewModels;
using ArchsVsDinosClient.Views.MatchViews;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
    public partial class Lobby : Window
    {

        private readonly LobbyViewModel viewModel;
        private readonly Label[] usernameLabels;
        private readonly Label[] nicknameLabels;

        public Lobby()
        {
            InitializeComponent();
            viewModel = new LobbyViewModel();
            DataContext = viewModel;

            usernameLabels = new Label[] { Lb_P2Username, Lb_P3Username, Lb_P4Username };
            nicknameLabels = new Label[] { Lb_P2Nickname, Lb_P3Nickname, Lb_P4Nickname };

            Lb_P1Username.Content = viewModel.Players[0].Username;
            Lb_P1Nickname.Content = viewModel.Players[0].Nickname;

            viewModel.Players.CollectionChanged += Players_CollectionChanged;
        }

        private void Players_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (LobbyService.LobbyPlayerDTO player in e.NewItems)
                {
                    if (player.Username == UserSession.Instance.CurrentUser.Username)
                        continue;

                    for (int i = 0; i < usernameLabels.Length; i++)
                    {
                        if (string.IsNullOrEmpty(usernameLabels[i].Content.ToString()))
                        {
                            usernameLabels[i].Content = player.Username;
                            nicknameLabels[i].Content = player.Nickname;
                            break;
                        }
                    }
                }
            }
        }

        private void Click_BtnBegin(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            var match = new MainMatch(UserSession.Instance.CurrentUser.Username);
            match.Show();
            this.Close();
        }

        private void Click_BtnCancelMatch(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            var main = new MainWindow();
            main.Show();
            this.Close();
        }

        private void Click_BtnInviteFriends(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            Gr_MyFriends.Visibility = Visibility.Visible;
        }

        private void Click_BtnCancelInviteFriend(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            Gr_MyFriends.Visibility = Visibility.Collapsed;
        }

        private void Click_BtnInvitePlayerByEmail(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            Gr_InviteByEmail.Visibility = Visibility.Visible;
        }

        private void Click_BtnCancelInviteByEmail(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            Gr_InviteByEmail.Visibility = Visibility.Collapsed;
        }


    }
}
