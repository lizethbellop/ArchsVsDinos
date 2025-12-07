using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Utils;
using ArchsVsDinosClient.ViewModels;
using ArchsVsDinosClient.ViewModels.GameViewsModels;
using ArchsVsDinosClient.Views.MatchViews.MatchSeeDeck;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Effects;

namespace ArchsVsDinosClient.Views.MatchViews
{
    public partial class MainMatch : Window
    {
        private readonly ChatViewModel chatViewModel;
        private readonly GameViewModel gameViewModel;
        private readonly string currentUsername;
        private readonly List<LobbyPlayerDTO> playersInMatch;
        private readonly string gameMatchCode;
        private readonly int matchId;

        public MainMatch(List<LobbyPlayerDTO> players, string myUsername, string gameMatchCode)
        {
            InitializeComponent();

            currentUsername = myUsername;
            playersInMatch = players;
            this.gameMatchCode = gameMatchCode;
            var service = new GameServiceClient();
            matchId = ExtractMatchIdFromCode(gameMatchCode);

            try
            {
                chatViewModel = new ChatViewModel(new ChatServiceClient());
                Gr_Chat.DataContext = chatViewModel;
            }
            catch (EndpointNotFoundException)
            {
                MessageBox.Show("Chat service is not available. The game will continue without chat functionality.",
                    "Chat Unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (CommunicationException)
            {
                MessageBox.Show("Failed to initialize chat service. The game will continue without chat functionality.",
                    "Chat Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            try
            {
                gameViewModel = new GameViewModel(new GameServiceClient(), matchId, currentUsername, players);
                DataContext = gameViewModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show(Lang.GlobalSystemError);
                Close();
                return;
            }

            InitializePlayersVisuals(playersInMatch, currentUsername);
            Loaded += MatchLoaded;
        }

        private int ExtractMatchIdFromCode(string gameMatchCode)
        {
            if (string.IsNullOrWhiteSpace(gameMatchCode))
                throw new ArgumentException(Lang.GlobalSystemError);

            return Math.Abs(gameMatchCode.GetHashCode());
        }

        private void InitializePlayersVisuals(List<LobbyPlayerDTO> players, string myUsername)
        {
            var others = players.Where(player => player.Username != myUsername).ToList();

            if (others.Count > 0)
            {
                Lb_TopPlayerName.Content = others[0].Username;
            }

            if (others.Count > 1)
            {
                Lb_LeftPlayerName.Content = others[1].Username;
                Grid_LeftPlayer.Visibility = Visibility.Visible;
            }
            else
            {
                Grid_LeftPlayer.Visibility = Visibility.Collapsed; 
            }

            if (others.Count > 2)
            {
                Lb_RightPlayerName.Content = others[2].Username;
                Grid_RightPlayer.Visibility = Visibility.Visible; 
            }
            else
            {
                Grid_RightPlayer.Visibility = Visibility.Collapsed; 
            }
        }

        private void UpdateTurnGlow(string currentPlayerUsername)
        {
            Lb_TopPlayerName.Effect = null;
            Lb_LeftPlayerName.Effect = null;
            Lb_RightPlayerName.Effect = null;

            if (currentPlayerUsername == currentUsername) return;

            var neonGlow = new DropShadowEffect
            {
                Color = System.Windows.Media.Colors.Yellow, 
                Direction = 0,
                ShadowDepth = 0,
                BlurRadius = 25, 
                Opacity = 1
            };

            if (Lb_TopPlayerName.Content.ToString() == currentPlayerUsername)
            {
                Lb_TopPlayerName.Effect = neonGlow;
            }
            else if (Grid_LeftPlayer.Visibility == Visibility.Visible &&
                     Lb_LeftPlayerName.Content.ToString() == currentPlayerUsername)
            {
                Lb_LeftPlayerName.Effect = neonGlow;
            }
            else if (Grid_RightPlayer.Visibility == Visibility.Visible &&
                     Lb_RightPlayerName.Content.ToString() == currentPlayerUsername)
            {
                Lb_RightPlayerName.Effect = neonGlow;
            }
        }

        private async void MatchLoaded(object sender, RoutedEventArgs e)
        {
            if (chatViewModel != null)
            {
                try
                {
                    await chatViewModel.ConnectAsync(currentUsername).ConfigureAwait(true);
                }
                catch (EndpointNotFoundException)
                {
                    MessageBox.Show("Chat server is not reachable.", "Connection Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (TimeoutException)
                {
                    MessageBox.Show("Connection to chat server timed out.", "Timeout Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (CommunicationException)
                {
                    MessageBox.Show("Failed to connect to chat server.", "Connection Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            if (gameViewModel != null)
            {
                gameViewModel.BoardManager.PlayerHand.CollectionChanged += PlayerHandCollectionChanged;
                gameViewModel.TurnChangedForUI += UpdateTurnGlow;
                MyDeckCanvas.SizeChanged += (s, args) => UpdatePlayerHandVisual();
                await gameViewModel.InitializeAndStartGameAsync();
                await Application.Current.Dispatcher.InvokeAsync(() => UpdatePlayerHandVisual(),
                    System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private void Click_BtnSeeDeckP1(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new MatchSeeDeckHorizontal().ShowDialog();
        }

        private void Click_BtnSeeDeckP2(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new MatchSeeDeckHorizontal().ShowDialog();
        }

        private void Click_BtnSeeDeckP3(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new MatchSeeDeckHorizontal().ShowDialog();
        }

        private void Click_BtnSeeDeckP4(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new MatchSeeDeckHorizontal().ShowDialog();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void Click_BtnChat(object sender, RoutedEventArgs e)
        {
            Gr_Chat.Visibility = Visibility.Visible;
            Btn_Chat.Visibility = Visibility.Collapsed;
        }

        private void Click_BtnClose(object sender, RoutedEventArgs e)
        {
            Gr_Chat.Visibility = Visibility.Collapsed;
            Btn_Chat.Visibility = Visibility.Visible;
        }

        private void PlayerHandCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdatePlayerHandVisual();
            });
        }

        private void UpdatePlayerHandVisual()
        {
            double canvasWidth = MyDeckCanvas.ActualWidth > 0 ? MyDeckCanvas.ActualWidth : 800;

            MyDeckCanvas.Children.Clear();

            var cards = gameViewModel.BoardManager.PlayerHand.ToList();
            if (cards.Count == 0) return;

            double cardWidth = 80;
            double overlap = 50;
            double totalWidth = (cards.Count - 1) * overlap + cardWidth;


            double startX = (canvasWidth - totalWidth) / 2;

            if (startX < 10) startX = 10; 

            for (int i = 0; i < cards.Count; i++)
            {
                var cardControl = new DeckCardSelection
                {
                    Card = cards[i]
                };

                double leftPosition = startX + (i * overlap);
                cardControl.SetInitialPosition(leftPosition, -70);
                Panel.SetZIndex(cardControl, i);
                MyDeckCanvas.Children.Add(cardControl);
            }
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            if (chatViewModel != null)
            {
                try
                {
                    await chatViewModel.DisconnectAsync().ConfigureAwait(true);
                    chatViewModel.Dispose();
                }
                catch { }
            }

            if (gameViewModel != null)
            {
                try
                {
                    gameViewModel.BoardManager.PlayerHand.CollectionChanged -= PlayerHandCollectionChanged;
                    gameViewModel.Dispose();
                }
                catch { }
            }

            base.OnClosing(e);
        }
    }
}