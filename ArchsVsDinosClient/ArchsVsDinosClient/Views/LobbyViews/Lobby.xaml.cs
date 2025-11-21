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

namespace ArchsVsDinosClient.Views.LobbyViews
{
    public partial class Lobby : Window
    {

        private readonly LobbyViewModel viewModel;

        public Lobby()
        {
            InitializeComponent();
            viewModel = new LobbyViewModel();
            DataContext = viewModel;
            viewModel.MatchCodeReceived += code =>
            {
                Lb_MatchCode.Content = code;
            };
        }

        private void Click_BtnBegin(object sender, RoutedEventArgs e)
        {
            if (!viewModel.CurrentClientIsHost())
            {
                MessageBox.Show(Lang.Lobby_LobbyBeginHost);
                return;
            }

            SoundButton.PlayDestroyingRockSound();
            var match = new MainMatch(UserSession.Instance.CurrentUser.Username);
            match.Show();
            this.Close();
        }


        private void Click_BtnCancelMatch(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();

            if (viewModel.CurrentClientIsHost())
            {
                var result = MessageBox.Show(Lang.Lobby_CancellationLobbyConfirmation, Lang.GlobalAcceptText, MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    viewModel.CancellLobby(UserSession.Instance.CurrentUser.Username, viewModel.MatchCode);
                }
            }
            else
            {
                viewModel.LeaveLobby(UserSession.Instance.CurrentUser.Username);
            }

            var main = new MainWindow();
            main.Show();
            this.Close();
        }

        private void Click_BtnExpelPlayer(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var usernameToExpel = (string)button.Tag;

            if (!viewModel.CurrentClientIsHost())
            {
                MessageBox.Show(Lang.Lobby_OnlyHostCanKick);
                return;
            }

            var confirm = MessageBox.Show($"{Lang.Lobby_QuestKick} {usernameToExpel}?", Lang.GlobalAcceptText, MessageBoxButton.YesNo);
            if (confirm != MessageBoxResult.Yes) return;

            viewModel.ExpelPlayer(UserSession.Instance.CurrentUser.Username, usernameToExpel);
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
