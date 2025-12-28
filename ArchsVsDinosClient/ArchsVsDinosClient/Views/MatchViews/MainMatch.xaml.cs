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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ArchsVsDinosClient.Views.MatchViews
{
    public partial class MainMatch : Window
    {
        private readonly ChatViewModel chatViewModel;
        private readonly GameViewModel gameViewModel;
        private readonly string currentUsername;
        private readonly List<LobbyPlayerDTO> playersInMatch;
        private readonly string gameMatchCode;

        private DispatcherTimer errorNotificationTimer;
        private CardCell lastHoveredCardCell;

        public MainMatch(List<LobbyPlayerDTO> players, string myUsername, string gameMatchCode, int myLobbyUserId)
        {
            InitializeComponent();
            this.currentUsername = myUsername;
            this.playersInMatch = players;
            this.gameMatchCode = gameMatchCode;

            try
            {
                chatViewModel = new ChatViewModel(new ChatServiceClient());
                Gr_Chat.DataContext = chatViewModel;
            }
            catch (Exception)
            {
                MessageBox.Show("Chat no disponible.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            try
            {
                var gameService = new GameServiceClient();
                gameViewModel = new GameViewModel(gameService, this.gameMatchCode, currentUsername, players, myLobbyUserId);
                DataContext = gameViewModel;
            }
            catch (Exception)
            {
                MessageBox.Show(Lang.GlobalSystemError);
                Close();
                return;
            }

            InitializePlayersVisuals(playersInMatch, currentUsername);
            InitializeErrorTimer();
            InitializeDragAndDrop();

            gameViewModel.PropertyChanged += GameViewModelPropertyChanged;
            Loaded += MatchLoaded;
        }

        private async void MatchLoaded(object sender, RoutedEventArgs e)
        {
            if (chatViewModel != null)
            {
                try
                {
                    await chatViewModel.ConnectAsync(currentUsername, context: 0, matchCode: gameMatchCode);
                }
                catch
                {

                }
            }

            if (gameViewModel != null)
            {
                gameViewModel.BoardManager.PlayerHand.CollectionChanged += PlayerHandCollectionChanged;
                gameViewModel.TurnChangedForUI += UpdateTurnGlow;
                gameViewModel.ArchCardPlaced += ShowArchPlacedAnimation;
                MyDeckCanvas.SizeChanged += (s, args) => UpdatePlayerHandVisual();

                await gameViewModel.ConnectToGameAsync();

                await Application.Current.Dispatcher.InvokeAsync(() => UpdatePlayerHandVisual(), DispatcherPriority.Loaded);
            }

            if (chatViewModel != null)
            {
                try
                {
                    await Task.Delay(1000);

                    await chatViewModel.ConnectAsync(currentUsername, context: 0, matchCode: gameMatchCode);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error conecting chat in match: {ex.Message}");
                }
            }
        }

        public void ManualDragOver(Point windowMousePosition, Card cardBeingDragged)
        {
            CardCell cardCellUnderMouse = FindCellUnderMouse(windowMousePosition);

            if (lastHoveredCardCell != null && lastHoveredCardCell != cardCellUnderMouse)
            {
                ClearAllCellEffects(lastHoveredCardCell);
            }

            lastHoveredCardCell = cardCellUnderMouse;

            if (cardCellUnderMouse != null)
            {
                ClearAllCellEffects(cardCellUnderMouse);

                string logicError = gameViewModel.ActionManager.ValidateDrop(
                    cardBeingDragged,
                    cardCellUnderMouse.CellId,
                    gameViewModel.RemainingMoves,
                    gameViewModel.IsMyTurn);

                bool isLogicallyValid = logicError == null;
                int targetSubIndex = GetCorrectIndexForCard(cardBeingDragged);

                Color highlightColor;
                if (isLogicallyValid)
                {
                    highlightColor = Colors.Lime;
                }
                else
                {
                    highlightColor = Colors.Red;
                }

                if (targetSubIndex != -1)
                {
                    ApplyNeonEffect(cardCellUnderMouse, targetSubIndex, highlightColor);
                }
            }
        }

        public async Task<bool> ManualDrop(Point windowMousePosition, Card cardBeingDropped)
        {
            CardCell targetCardCell = FindCellUnderMouse(windowMousePosition);

            if (lastHoveredCardCell != null)
            {
                ClearAllCellEffects(lastHoveredCardCell);
                lastHoveredCardCell = null;
            }

            if (targetCardCell != null)
            {
                string errorMessage = await gameViewModel.TryPlayCardAsync(cardBeingDropped, targetCardCell.CellId);

                if (errorMessage == null)
                {
                    PlaceCardImageInGrid(targetCardCell, cardBeingDropped);
                    Gr_ErrorNotification.Visibility = Visibility.Collapsed;
                    return true;
                }
                else
                {
                    ShowErrorNotification(errorMessage);
                    return false;
                }
            }

            return false;
        }

        private async void Click_BtnTakeACard(object sender, RoutedEventArgs e)
        {
            const int mainDrawPileIndex = 0;

            if (sender is Button button)
            {
                button.IsEnabled = false;
                string errorMessage = await gameViewModel.ExecuteDrawCardFromView(mainDrawPileIndex);

                if (errorMessage != null)
                {
                    ShowErrorNotification(errorMessage);
                }
                CheckDrawButtonState();
            }
        }

        private int GetCorrectIndexForCard(Card card)
        {
            if (card.Category == CardCategory.DinoHead)
            {
                return 2;
            }

            if (card.Category == CardCategory.BodyPart)
            {
                switch (card.BodyPartType)
                {
                    case BodyPartType.LeftArm:
                        return 4;
                    case BodyPartType.Chest:
                        return 5;
                    case BodyPartType.RightArm:
                        return 6;
                    case BodyPartType.Legs:
                        return 8;
                }
            }
            return -1;
        }

        private CardCell FindCellUnderMouse(Point windowMousePosition)
        {
            Point screenMousePos = this.PointToScreen(windowMousePosition);

            for (int i = 1; i <= 6; i++)
            {
                var cell = BottomPlayerCards.GetCombinationCell(i);
                if (cell == null)
                {
                    continue;
                }

                Point cellScreenPos = cell.PointToScreen(new Point(0, 0));

                if (screenMousePos.X >= cellScreenPos.X &&
                    screenMousePos.X <= cellScreenPos.X + cell.ActualWidth &&
                    screenMousePos.Y >= cellScreenPos.Y &&
                    screenMousePos.Y <= cellScreenPos.Y + cell.ActualHeight)
                {
                    return cell;
                }
            }
            return null;
        }

        private void ApplyNeonEffect(CardCell cell, int index, Color color)
        {
            if (index != 2 && index != 4 && index != 5 && index != 6 && index != 8)
            {
                return;
            }

            var targetBorder = cell.GetSubCell(index);
            if (targetBorder != null)
            {
                targetBorder.Effect = new DropShadowEffect
                {
                    Color = color,
                    Direction = 0,
                    ShadowDepth = 0,
                    BlurRadius = 25,
                    Opacity = 1
                };
            }
        }

        private void ClearAllCellEffects(CardCell cell)
        {
            int[] validIndices = { 2, 4, 5, 6, 8 };
            foreach (var i in validIndices)
            {
                var sub = cell.GetSubCell(i);
                if (sub != null)
                {
                    sub.Effect = null;
                }
            }
        }

        private void PlaceCardImageInGrid(CardCell cell, Card card)
        {
            Border targetBorder = null;

            if (card.Category == CardCategory.DinoHead)
            {
                targetBorder = cell.Part_Head;
            }
            else if (card.BodyPartType == BodyPartType.Chest)
            {
                targetBorder = cell.Part_Chest;
            }
            else if (card.BodyPartType == BodyPartType.LeftArm)
            {
                targetBorder = cell.Part_LeftArm;
            }
            else if (card.BodyPartType == BodyPartType.RightArm)
            {
                targetBorder = cell.Part_RightArm;
            }
            else if (card.BodyPartType == BodyPartType.Legs)
            {
                targetBorder = cell.Part_Legs;
            }

            if (targetBorder != null)
            {
                var brush = new ImageBrush(new BitmapImage(new Uri(card.CardRoute)));
                targetBorder.Background = brush;
            }
        }
        /*
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
                }*/

        private void InitializePlayersVisuals(List<LobbyPlayerDTO> players, string myUsername)
        {
            string myNickname = UserSession.Instance.GetNickname();
            var others = players.Where(player =>
                player.Username != myUsername &&
                player.Nickname != myNickname
            ).ToList();

            if (others.Count > 0)
            {
                Lb_TopPlayerName.Content = others[0].Nickname ?? others[0].Username;
            }

            if (others.Count > 1)
            {
                Lb_LeftPlayerName.Content = others[1].Nickname ?? others[1].Username;
                Grid_LeftPlayer.Visibility = Visibility.Visible;
            }
            else
            {
                Grid_LeftPlayer.Visibility = Visibility.Collapsed;
            }

            if (others.Count > 2)
            {
                Lb_RightPlayerName.Content = others[2].Nickname ?? others[2].Username;
                Grid_RightPlayer.Visibility = Visibility.Visible;
            }
            else
            {
                Grid_RightPlayer.Visibility = Visibility.Collapsed;
            }
        }

        private void InitializeErrorTimer()
        {
            errorNotificationTimer = new DispatcherTimer();
            errorNotificationTimer.Interval = TimeSpan.FromSeconds(3);
            errorNotificationTimer.Tick += (s, e) =>
            {
                errorNotificationTimer.Stop();
                FadeOutErrorNotification();
            };
        }

        private void InitializeDragAndDrop()
        {
            for (int i = 1; i <= 6; i++)
            {
                var cell = BottomPlayerCards.GetCombinationCell(i);
                if (cell != null)
                {
                    cell.AllowDrop = true;
                    cell.Drop += OnCardDrop;
                    cell.DragOver += OnCardDragOver;
                    cell.DragLeave += OnCardDragLeave;
                }
            }
        }

        private void PlayerHandCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Diagnostics.Debug.WriteLine($"[UI] ========== PlayerHand Collection Changed ==========");
                System.Diagnostics.Debug.WriteLine($"[UI] Action: {e.Action}");

                if (e.NewItems != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[UI] New items count: {e.NewItems.Count}");
                    foreach (Card card in e.NewItems)
                    {
                        System.Diagnostics.Debug.WriteLine($"[UI] - Added card: {card.IdCard} ({card.Category})");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[UI] Total cards in PlayerHand: {gameViewModel.BoardManager.PlayerHand.Count}");

                UpdatePlayerHandVisual();
                CheckDrawButtonState();

                System.Diagnostics.Debug.WriteLine($"[UI] ================================================");
            });
        }

        /*private void PlayerHandCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdatePlayerHandVisual();
                CheckDrawButtonState();
            });
        }*/

        private void OnCardDragLeave(object sender, DragEventArgs e)
        {
            if (sender is CardCell cell)
            {
                ClearAllCellEffects(cell);
            }
        }

        private void OnCardDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(Card)) || !(sender is CardCell))
            {
                e.Effects = DragDropEffects.None;
                return;
            }
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private async void OnCardDrop(object sender, DragEventArgs e)
        {
            if (sender is CardCell cell && e.Data.GetDataPresent(typeof(Card)))
            {
                ClearAllCellEffects(cell);
                var card = (Card)e.Data.GetData(typeof(Card));

                string error = await gameViewModel.TryPlayCardAsync(card, cell.CellId);
                if (error == null)
                {
                    PlaceCardImageInGrid(cell, card);
                }
                else
                {
                    ShowErrorNotification(error);
                }
            }
        }

        private void GameViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GameViewModel.RemainingMoves) ||
              e.PropertyName == nameof(GameViewModel.IsMyTurn) ||
              e.PropertyName == nameof(GameViewModel.RemainingCardsInDeck))
            {
                Application.Current.Dispatcher.Invoke(() => CheckDrawButtonState());
            }
        }

        private void CheckDrawButtonState()
        {
            var drawButton = Gr_AllCards.FindName("Btn_TakeACard") as Button;
            if (drawButton == null)
            {
                return;
            }

            if (gameViewModel.RemainingCardsInDeck <= 0)
            {
                drawButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                drawButton.Visibility = Visibility.Visible;
            }

            if (gameViewModel.IsMyTurn && gameViewModel.RemainingMoves > 0)
            {
                drawButton.IsEnabled = true;
            }
            else
            {
                drawButton.IsEnabled = false;
            }
        }

        private void ShowErrorNotification(string message)
        {
            errorNotificationTimer.Stop();
            Gr_ErrorNotification.BeginAnimation(OpacityProperty, null);
            TxtB_ErrorNotificationContainer.Text = message;
            Gr_ErrorNotification.Visibility = Visibility.Visible;
            Gr_ErrorNotification.Opacity = 0.9;
            errorNotificationTimer.Start();
        }

        private void FadeOutErrorNotification()
        {
            var fadeOut = new DoubleAnimation
            {
                From = 0.9,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromSeconds(0.5))
            };
            fadeOut.Completed += (s, e) => Gr_ErrorNotification.Visibility = Visibility.Collapsed;
            Gr_ErrorNotification.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void UpdateTurnGlow(string currentPlayerUsername)
        {
            ResetPlayerStyle(Lb_TopPlayerName);
            ResetPlayerStyle(Lb_LeftPlayerName);
            ResetPlayerStyle(Lb_RightPlayerName);

            if (currentPlayerUsername == currentUsername)
            {
                return;
            }

            if (IsPlayerName(Lb_TopPlayerName, currentPlayerUsername))
            {
                HighlightPlayer(Lb_TopPlayerName);
            }
            else if (Grid_LeftPlayer.Visibility == Visibility.Visible && IsPlayerName(Lb_LeftPlayerName, currentPlayerUsername))
            {
                HighlightPlayer(Lb_LeftPlayerName);
            }
            else if (Grid_RightPlayer.Visibility == Visibility.Visible && IsPlayerName(Lb_RightPlayerName, currentPlayerUsername))
            {
                HighlightPlayer(Lb_RightPlayerName);
            }
        }

        private bool IsPlayerName(Label label, string username)
        {
            return label.Content != null && label.Content.ToString() == username;
        }

        private void ResetPlayerStyle(Label label)
        {
            label.Foreground = Brushes.White;
            label.Effect = null;
        }

        private void HighlightPlayer(Label label)
        {
            label.Foreground = Brushes.Yellow;
            label.Effect = new DropShadowEffect
            {
                Color = Colors.Yellow,
                Direction = 0,
                ShadowDepth = 0,
                BlurRadius = 15,
                Opacity = 1
            };
        }
        /*
        public void UpdatePlayerHandVisual()
        {
            double canvasWidth = MyDeckCanvas.ActualWidth > 0 ? MyDeckCanvas.ActualWidth : 800;
            MyDeckCanvas.Children.Clear();

            var cards = gameViewModel.BoardManager.PlayerHand.ToList();
            if (cards.Count == 0)
            {
                return;
            }

            double cardWidth = 80;
            double overlap = 50;
            double totalWidth = (cards.Count - 1) * overlap + cardWidth;
            double startX = (canvasWidth - totalWidth) / 2;

            if (startX < 10)
            {
                startX = 10;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                var cardControl = new DeckCardSelection { Card = cards[i] };
                double leftPosition = startX + (i * overlap);
                cardControl.SetInitialPosition(leftPosition, -70);
                Panel.SetZIndex(cardControl, i);
                MyDeckCanvas.Children.Add(cardControl);
            }
        }*/

        public void UpdatePlayerHandVisual()
        {
            if (MyDeckCanvas == null)
            {
                System.Diagnostics.Debug.WriteLine("[UI] ❌ MyDeckCanvas is NULL!");
                return;
            }

            double canvasWidth = MyDeckCanvas.ActualWidth > 0 ? MyDeckCanvas.ActualWidth : 800;
            System.Diagnostics.Debug.WriteLine($"[UI] Canvas width: {canvasWidth}");

            MyDeckCanvas.Children.Clear();
            System.Diagnostics.Debug.WriteLine($"[UI] Cleared canvas. Rebuilding with {gameViewModel.BoardManager.PlayerHand.Count} cards");

            var cards = gameViewModel.BoardManager.PlayerHand.ToList();
            if (cards.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[UI] No cards to display");
                return;
            }

            double cardWidth = 80;
            double overlap = 50;
            double totalWidth = (cards.Count - 1) * overlap + cardWidth;
            double startX = (canvasWidth - totalWidth) / 2;

            if (startX < 10)
            {
                startX = 10;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                var cardControl = new DeckCardSelection { Card = cards[i] };
                double leftPosition = startX + (i * overlap);
                cardControl.SetInitialPosition(leftPosition, -70);
                Panel.SetZIndex(cardControl, i);
                MyDeckCanvas.Children.Add(cardControl);

                System.Diagnostics.Debug.WriteLine($"[UI] Added card {cards[i].IdCard} at position {i}, left={leftPosition}");
            }

            System.Diagnostics.Debug.WriteLine($"[UI] Total controls in canvas: {MyDeckCanvas.Children.Count}");
        }

        private void ShowArchPlacedAnimation(string armyName, int cardId)
        {
            var notification = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 75, 0, 130)),
                BorderBrush = new SolidColorBrush(Colors.Gold),
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(20),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 100, 0, 0),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Gold,
                    ShadowDepth = 0,
                    BlurRadius = 30,
                    Opacity = 1
                }
            };

            var text = new TextBlock
            {
                Text = $"{Lang.Match_ArchAddedMessage} {armyName}",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.Gold),
                TextAlignment = TextAlignment.Center
            };

            notification.Child = text;

            var mainGrid = this.Content as Grid;
            if (mainGrid != null)
            {
                mainGrid.Children.Add(notification);
                Panel.SetZIndex(notification, 1000);

                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.5)
                };

                notification.BeginAnimation(OpacityProperty, fadeIn);

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();

                    var fadeOut = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.5)
                    };

                    fadeOut.Completed += (ss, ee) => mainGrid.Children.Remove(notification);
                    notification.BeginAnimation(OpacityProperty, fadeOut);
                };
                timer.Start();
            }
        }

        private void Click_BtnSeeDeckP1(object sender, RoutedEventArgs e)
        {
            ShowDeckWindow();
        }

        private void Click_BtnSeeDeckP2(object sender, RoutedEventArgs e)
        {
            ShowDeckWindow();
        }

        private void Click_BtnSeeDeckP3(object sender, RoutedEventArgs e)
        {
            ShowDeckWindow();
        }

        private void Click_BtnSeeDeckP4(object sender, RoutedEventArgs e)
        {
            ShowDeckWindow();
        }

        private async void Click_BtnEndTurn(object sender, RoutedEventArgs e)
        {
            await gameViewModel.EndTurnManuallyAsync();
        }
        private void ShowDeckWindow()
        {
            SoundButton.PlayMovingRockSound();
            new MatchSeeDeckHorizontal().ShowDialog();
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

        protected override async void OnClosing(CancelEventArgs e)
        {
            if (chatViewModel != null)
            {
                try
                {
                    await chatViewModel.DisconnectAsync();
                    chatViewModel.Dispose();
                }
                catch
                {

                }
            }

            if (gameViewModel != null)
            {
                try
                {
                    gameViewModel.BoardManager.PlayerHand.CollectionChanged -= PlayerHandCollectionChanged;
                    gameViewModel.Dispose();
                }
                catch
                {

                }
            }
            base.OnClosing(e);
        }
    }
}