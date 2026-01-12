using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using ArchsVsDinosClient.ViewModels;
using ArchsVsDinosClient.Views.MatchViews;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ArchsVsDinosClient.Views.LobbyViews
{
    public partial class Lobby : BaseSessionWindow
    {
        private readonly LobbyViewModel viewModel;
        private readonly string currentUsername;

        private bool handledConnectionLost;
        private bool isNavigatingToGame;
        private bool isExitCleanupRunning;
        private bool hasExitCleanupCompleted;

        public Lobby() : this(true)
        {
        }

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

            Loaded += async (_, __) => await viewModel.LoadFriendsAsync();

            Loaded += async (_, __) =>
            {
                if (!isHost)
                {
                    return;
                }

                bool success = await viewModel.InitializeLobbyAsync();
                if (!success)
                {
                    Close();
                }
            };

            Closing += OnLobbyClosing;

            ExtraCleanupAction = RunNavigationCleanupAsync;
        }
        private void RequestCloseAfterCleanup()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LOBBY] Close after cleanup failed: {ex}");
                    Application.Current.Shutdown();
                }
            }));
        }

        private async void OnLobbyClosing(object sender, CancelEventArgs e)
        {
            if (IsNavigating || hasExitCleanupCompleted)
            {
                return;
            }

            if (isExitCleanupRunning)
            {
                e.Cancel = true;
                return;
            }

            e.Cancel = true;
            isExitCleanupRunning = true;

            try
            {
                await RunExitCleanupAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOBBY] Exit cleanup failed: {ex}");
            }
            finally
            {
                hasExitCleanupCompleted = true;
                isExitCleanupRunning = false;

                RequestCloseAfterCleanup();
            }
        }


        private async Task RunExitCleanupAsync()
        {
            if (viewModel == null)
            {
                return;
            }

            viewModel.CancelReconnectionAndExit();

            UnsubscribeFromViewModelEvents();

            try
            {
                await viewModel.CleanupBeforeClosingAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOBBY] CleanupBeforeClosingAsync error: {ex.Message}");
                viewModel.Cleanup();
            }
        }

        private async Task RunNavigationCleanupAsync()
        {
            if (viewModel == null)
            {
                return;
            }

            try
            {
                viewModel.CancelReconnectionAndExit();

                if (viewModel.Chat != null)
                {
                    await viewModel.Chat.DisconnectAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOBBY] Navigation cleanup error: {ex.Message}");
            }
        }

        private void UnsubscribeFromViewModelEvents()
        {
            viewModel.LobbyConnectionLost -= OnLobbyConnectionLost;
            viewModel.NavigateToGame -= OnNavigateToGame;
            viewModel.NavigateToLobbyAsGuest -= OnNavigateToLobbyAsGuest;
        }

        private void OnNavigateToLobbyAsGuest(string lobbyCode)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    Debug.WriteLine($"[LOBBY] Navigating to new lobby as guest: {lobbyCode}");

                    IsNavigating = true;

                    var lobbyServiceClient = new LobbyServiceClient();
                    var newLobbyWindow = new Lobby(false, lobbyServiceClient);

                    Application.Current.MainWindow = newLobbyWindow;
                    newLobbyWindow.Show();

                    Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LOBBY] Error navigating to new lobby: {ex.Message}");

                    MessageBox.Show(
                        Lang.GlobalUnexpectedError,
                        Lang.GlobalError,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            });
        }

        private void OnLobbyConnectionLost(string title, string message)
        {
            if (handledConnectionLost)
            {
                return;
            }

            handledConnectionLost = true;

            _ = Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    IsNavigating = true;

                    await RunExitCleanupAsync();

                    MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

                    // ✨ ESTO MANDA A LOGIN (con logout)
                    NavigateToLogin();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LOBBY] Error handling connection lost: {ex.Message}");
                    NavigateToLogin();
                }
            });
        }

        private void NavigateToLogin()
        {
            try
            {
                const bool SHOULD_LOGOUT = true;

                ForceLogoutOnClose = SHOULD_LOGOUT;
                IsNavigating = true;

                Window oldMainWindow = Application.Current.MainWindow;

                var loginWindow = new Login();
                Application.Current.MainWindow = loginWindow;
                loginWindow.Show();

                if (oldMainWindow != null &&
                    oldMainWindow != this &&
                    oldMainWindow != loginWindow)
                {
                    oldMainWindow.Close();
                }

                Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOBBY] Error navigating to Login: {ex.Message}");

                MessageBox.Show(
                    Lang.GlobalUnexpectedError,
                    Lang.GlobalError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void NavigateToMainWindow()
        {
            try
            {
                const bool SHOULD_LOGOUT = false;

                ForceLogoutOnClose = SHOULD_LOGOUT;
                IsNavigating = true;

                Window oldMainWindow = Application.Current.MainWindow;

                var mainWindow = new MainWindow();
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();

                if (oldMainWindow != null &&
                    oldMainWindow != this &&
                    oldMainWindow != mainWindow)
                {
                    oldMainWindow.Close();
                }

                Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOBBY] Error navigating to MainWindow: {ex.Message}");

                MessageBox.Show(
                    Lang.GlobalUnexpectedError,
                    Lang.GlobalError,
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

            Dispatcher.Invoke(() =>
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

                    List<DTO.LobbyPlayerDTO> players = viewModel.GetCurrentPlayers();
                    int myLobbyUserId = viewModel.GetMyLobbyUserId();

                    IsNavigating = true;

                    var gameWindow = new MainMatch(players, myUsername, matchCode, myLobbyUserId);
                    Application.Current.MainWindow = gameWindow;
                    gameWindow.Show();

                    Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LOBBY] Error navigating to game: {ex.Message}");
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
                return;
            }

            SoundButton.PlayDestroyingRockSound();
            viewModel.StartTheGame(viewModel.MatchCode, UserSession.Instance.CurrentUser.Username);
        }

        private async void Click_BtnCancelMatch(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();

            if (UserSession.Instance.CurrentUser == null)
            {
                Close();
                return;
            }

            viewModel.CancelReconnectionAndExit();

            if (viewModel.CurrentClientIsHost())
            {
                var result = MessageBox.Show(
                    Lang.Lobby_CancellationLobbyConfirmation,
                    Lang.GlobalAcceptText,
                    MessageBoxButton.YesNo
                );

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            IsNavigating = true;

            try
            {
                await RunExitCleanupAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOBBY] CancelMatch cleanup error: {ex.Message}");
            }

            NavigateToMainWindow();
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
                        Lang.GlobalInformation,
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
                    Lang.GlobalUnexpectedError,
                    Lang.GlobalError,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void Click_BtnInviteFriend(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
            {
                return;
            }

            string friendUsername = button.Tag as string;
            if (string.IsNullOrEmpty(friendUsername))
            {
                return;
            }

            SoundButton.PlayMovingRockSound();
            viewModel.InviteFriendToLobby(friendUsername);
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
