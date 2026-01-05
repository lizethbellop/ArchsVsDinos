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
using System.Diagnostics;
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
    public partial class Lobby : BaseSessionWindow
    {
        private readonly LobbyViewModel viewModel;
        private string currentUsername;
        private bool handledConnectionLost = false;
        private bool isNavigatingToGame = false;

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

            viewModel.LobbyConnectionLost += OnLobbyConnectionLost;
            viewModel.NavigateToGame += OnNavigateToGame;
            viewModel.NavigateToLobbyAsGuest += OnNavigateToLobbyAsGuest;

            this.Loaded += async (s, e) =>
            {
                await viewModel.LoadFriendsAsync();
            };

            Loaded += async (_, __) =>
            {
                if (isHost)
                {
                    bool success = await viewModel.InitializeLobbyAsync();

                    if (!success)
                    {
                        Close();
                    }
                }
            };

            this.ExtraCleanupAction = async () =>
            {
                if (viewModel != null)
                {
                    viewModel.CancelReconnectionAndExit();
                    if (viewModel.Chat != null)
                    {
                        try { await viewModel.Chat.DisconnectAsync(); } catch { }
                    }
                    viewModel.LobbyConnectionLost -= OnLobbyConnectionLost;
                    viewModel.NavigateToGame -= OnNavigateToGame;
                    viewModel.NavigateToLobbyAsGuest -= OnNavigateToLobbyAsGuest;
                    viewModel.Cleanup();
                }
            };
        }

        private void OnNavigateToLobbyAsGuest(string lobbyCode)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    Debug.WriteLine($"[LOBBY] Navigating to new lobby as guest: {lobbyCode}");

                    var lobbyServiceClient = new LobbyServiceClient();
                    Lobby newLobbyWindow = new Lobby(false, lobbyServiceClient);

                    newLobbyWindow.Show();
                    this.Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LOBBY] Error navigating to new lobby: {ex.Message}");
                    MessageBox.Show(
                        "Error al abrir el nuevo lobby.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            });
        }

        private void OnLobbyConnectionLost(string title, string message)
        {
            if (handledConnectionLost)
                return;

            handledConnectionLost = true;

            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

                NavigateToMainWindow();
            });
        }

        private void NavigateToMainWindow()
        {
            try
            {
                MainWindow newMainWindow = new MainWindow();

                var oldMain = Application.Current.MainWindow;
                if (oldMain != null && oldMain != this)
                {
                    oldMain.Close();
                }

                Application.Current.MainWindow = newMainWindow;
                newMainWindow.Show();

                this.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOBBY] Error al navegar a MainWindow: {ex.Message}");
                MessageBox.Show(
                    "Error al regresar al menú principal.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void OnNavigateToGame()
        {
            if (isNavigatingToGame)
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {

                if (isNavigatingToGame)
                {
                    return;
                }

                isNavigatingToGame = true;

                try
                {
                    string matchCode = viewModel.MatchCode;
                    string myUsername = UserSession.Instance.CurrentUser.Username;
                    List<ArchsVsDinosClient.DTO.LobbyPlayerDTO> players = viewModel.GetCurrentPlayers();
                    int myLobbyUserId = viewModel.GetMyLobbyUserId();

                    this.IsNavigating = true;
                    MainMatch gameWindow = new MainMatch(players, myUsername, matchCode, myLobbyUserId);
                    Application.Current.MainWindow = gameWindow;
                    gameWindow.Show();
                    this.Close();
                }
                catch (Exception)
                {
                    isNavigatingToGame = false;
                    MessageBox.Show(Lang.Lobby_ErrorStartingGame);
                }
            });
        }

        private async void LobbyLoaded(object sender, RoutedEventArgs e)
        {
            await viewModel.ConnectChatAsync();
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
        {
            SoundButton.PlayDestroyingRockSound();

            if (UserSession.Instance.CurrentUser == null)
            {
                this.Close();
                return;
            }

            string nicknameToSend = UserSession.Instance.GetNickname();

            viewModel?.CancelReconnectionAndExit();

            if (viewModel.CurrentClientIsHost())
            {
                var result = MessageBox.Show(
                    Lang.Lobby_CancellationLobbyConfirmation,
                    Lang.GlobalAcceptText,
                    MessageBoxButton.YesNo
                );

                if (result == MessageBoxResult.Yes)
                {
                    viewModel.LeaveOfTheLobby(nicknameToSend);
                    this.IsNavigating = true;
                    MainWindow main = new MainWindow();
                    Application.Current.MainWindow = main; 
                    main.Show();
                    this.Close();
                }
            }
            else
            {
                viewModel.LeaveOfTheLobby(nicknameToSend);
                this.IsNavigating = true;
                this.IsNavigating = true;
                MainWindow main = new MainWindow();
                Application.Current.MainWindow = main; 
                main.Show();
                this.Close();
            }
        }

        private async void Click_BtnInviteFriends(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();

            try
            {
                var friends = await viewModel.LoadFriendsAsync();

                if (friends == null || friends.Count == 0)
                {
                    MessageBox.Show(
                        "No tienes amigos agregados aún.",
                        "Sin amigos",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    return;
                }

                FriendsList.ItemsSource = friends;
                Gr_MyFriends.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOBBY] Error showing friends: {ex.Message}");
                MessageBox.Show(
                    "Error al cargar la lista de amigos.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void Click_BtnInviteFriend(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            string friendUsername = button.Tag as string;
            if (string.IsNullOrEmpty(friendUsername)) return;

            SoundButton.PlayMovingRockSound();
            viewModel?.InviteFriendToLobby(friendUsername);
        }

        private void Click_BtnCancelInviteFriend(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            Gr_MyFriends.Visibility = Visibility.Collapsed;
            FriendsList.ItemsSource = null;
        }

        private void Click_BtnInvitePlayerByEmail(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            Gr_InviteByEmail.Visibility = Visibility.Visible;
        }

        private void Click_BtnInviteAPlayerByEmail(object sender, RoutedEventArgs e)
        {
            string email = TxtB_InviteByEmail.Text.Trim();
            viewModel.InvitePlayerByEmail(email);
        }

        private void Click_BtnCancelInviteByEmail(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            Gr_InviteByEmail.Visibility = Visibility.Collapsed;
        }

    }
}
