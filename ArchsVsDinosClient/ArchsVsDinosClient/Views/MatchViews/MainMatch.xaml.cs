using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Utils;
using ArchsVsDinosClient.ViewModels;
using ArchsVsDinosClient.Views.MatchViews.MatchSeeDeck;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;

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

            InitializePlayers(playersInMatch, currentUsername);
            Loaded += Match_Loaded;
        }

        private int ExtractMatchIdFromCode(string gameMatchCode)
        {
            if (string.IsNullOrWhiteSpace(gameMatchCode))
                throw new ArgumentException(Lang.GlobalSystemError);

            return Math.Abs(gameMatchCode.GetHashCode());
        }

        private void InitializePlayers(List<LobbyPlayerDTO> players, string myUsername)
        {
            var others = players.Where(player => player.Username != myUsername).ToList();

            if (others.Count > 0)
                Lb_TopPlayerName.Content = others[0].Username;

            if (others.Count > 1)
                Lb_LeftPlayerName.Content = others[1].Username;

            if (others.Count > 2)
                Lb_RightPlayerName.Content = others[2].Username;
        }

        private async void Match_Loaded(object sender, RoutedEventArgs e)
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
                gameViewModel.PlayerHand.CollectionChanged += PlayerHand_CollectionChanged;
                await gameViewModel.InitializeAndStartGameAsync();
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

        private void PlayerHand_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdatePlayerHandVisual();
            });
        }

        private void UpdatePlayerHandVisual()
        {

            System.Diagnostics.Debug.WriteLine($"🎴 UPDATING VISUAL: {gameViewModel.PlayerHand.Count} cards");

            MyDeckCanvas.Children.Clear();

            var cards = gameViewModel.PlayerHand.ToList();
            if (cards.Count == 0) return;

            double cardWidth = 80;
            double overlap = 50; 
            double totalWidth = (cards.Count - 1) * overlap + cardWidth;

            double canvasWidth = MyDeckCanvas.ActualWidth > 0 ? MyDeckCanvas.ActualWidth : 800;
            double startX = (canvasWidth - totalWidth) / 2;

            for (int i = 0; i < cards.Count; i++)
            {
                var cardControl = new DeckCardSelection
                {
                    Card = cards[i]
                };

                double leftPosition = startX + (i * overlap);
                cardControl.SetInitialPosition(leftPosition, -80); 
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
                    gameViewModel.PlayerHand.CollectionChanged -= PlayerHand_CollectionChanged;
                    gameViewModel.Dispose();
                }
                catch { }
            }

            base.OnClosing(e);
        }
    }
}