using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.LobbyService;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using ArchsVsDinosClient.ViewModels;
using ArchsVsDinosClient.Views.MatchViews;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
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
        private string currentUsername;


        public Lobby() : this(true) { }

        public Lobby(bool isHost, ILobbyServiceClient client = null)
        {
            InitializeComponent();
            currentUsername = UserSession.Instance.CurrentUser.Username;

            if (client == null)
            {
                client = new LobbyServiceClient();
            }
            viewModel = new LobbyViewModel(isHost, client);
            DataContext = viewModel;



            if (isHost)
            {
                viewModel.InitializeLobby();
            }


        }


        private void Click_BtnBegin(object sender, RoutedEventArgs e)
        {
            int totalPlayers = viewModel.GetPlayersCount(); 

            if (totalPlayers < 2)
            {
                Btn_Begin.IsChecked = false;
                MessageBox.Show(Lang.Lobby_MiniumPlayers);
            }
            else
            {
                SoundButton.PlayDestroyingRockSound();
                viewModel.StartTheGame(viewModel.MatchCode, UserSession.Instance.CurrentUser.Username);
            }
            
        }


        private void Click_BtnCancelMatch(object sender, RoutedEventArgs e)
        {/*
            SoundButton.PlayDestroyingRockSound();

            if (UserSession.Instance.CurrentUser == null)
            {
                this.Close();
                return;
            }

            if (viewModel.CurrentClientIsHost())
            {
                var result = MessageBox.Show(
                    Lang.Lobby_CancellationLobbyConfirmation,
                    Lang.GlobalAcceptText,
                    MessageBoxButton.YesNo
                );

                if (result == MessageBoxResult.Yes)
                {
                    viewModel.CancellTheLobby(viewModel.MatchCode, UserSession.Instance.CurrentUser.Username);
                }
            }
            else
            {
                viewModel.LeaveOfTheLobby(UserSession.Instance.CurrentUser.Username);
                var main = new MainWindow();
                main.Show();
                this.Close();
            }*/

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

        private void Click_BtnInviteAPlayerByEmail(object sender, RoutedEventArgs e)
        {
            var email = TxtB_InviteByEmail.Text.Trim();
            viewModel.InvitePlayerByEmail(email);
        }

        private void Click_BtnCancelInviteByEmail(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            Gr_InviteByEmail.Visibility = Visibility.Collapsed;
        }

        private async void LobbyLoaded(object sender, RoutedEventArgs e)
        {
            await viewModel.ConnectChatAsync();
        }


    }
}
