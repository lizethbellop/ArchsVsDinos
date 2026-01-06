using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.GameService;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
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
    public partial class MainMatch : BaseSessionWindow
    {
        private readonly ChatViewModel chatViewModel;
        private readonly GameViewModel gameViewModel;
        private readonly string currentUsername;
        private readonly List<LobbyPlayerDTO> playersInMatch;
        private readonly string gameMatchCode;
        private Dictionary<string, int> playerPositionToUserId = new Dictionary<string, int>();
        private DispatcherTimer errorNotificationTimer;
        private CardCell lastHoveredCardCell;
        private bool isProvokeModeActive = false;
        private bool isHandlingDisconnection = false;

        private int player2UserId = 0;
        private int player3UserId = 0;
        private int player4UserId = 0;

        public MainMatch(List<LobbyPlayerDTO> players, string myUsername, string gameMatchCode, int myLobbyUserId)
        {
            InitializeComponent();
            this.currentUsername = myUsername;
            this.playersInMatch = players;
            this.gameMatchCode = gameMatchCode;

            MusicPlayer.Instance.StopBackgroundMusic();
            MusicPlayer.Instance.PlayBackgroundMusic(MusicTracks.Match);

            try
            {
                chatViewModel = new ChatViewModel(new ChatServiceClient());
                Gr_Chat.DataContext = chatViewModel;
            }
            catch (Exception)
            {
                MessageBox.Show(Lang.Match_ErrorChatNotAvailable);
            }

            try
            {
                var gameService = new GameServiceClient();
                gameViewModel = new GameViewModel(gameService, this.gameMatchCode, currentUsername, players, myLobbyUserId);
                DataContext = gameViewModel;

                gameViewModel.gameServiceClient.ServiceError += OnCriticalServiceError;
                gameViewModel.gameServiceClient.ConnectionLost += OnConnectionLost; // ← NUEVO
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

            this.ExtraCleanupAction = async () =>
            {
                if (chatViewModel != null)
                {
                    try
                    {
                        await chatViewModel.DisconnectAsync();
                        chatViewModel.Dispose();
                    }
                    catch { }
                }

                if (gameViewModel != null)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("[EXIT] Leaving match...");
                        await gameViewModel.LeaveGameAsync();

                        gameViewModel.BoardManager.PlayerHand.CollectionChanged -= PlayerHandCollectionChanged;
                        gameViewModel.TurnChangedForUI -= UpdateTurnGlow;
                        gameViewModel.ArchCardPlaced -= ShowArchPlacedAnimation;
                        gameViewModel.OpponentDinoHeadPlayed -= OnOpponentDinoHeadPlayed;
                        gameViewModel.OpponentBodyPartAttached -= OnOpponentBodyPartAttached;
                        gameViewModel.PlayerDinosClearedByElement -= OnPlayerDinosClearedByElement;
                        gameViewModel.DiscardPileUpdated -= OnDiscardPileUpdated;
                        gameViewModel.ArchArmyCleared -= OnArchArmyCleared;

                        gameViewModel.Dispose();
                    }
                    catch { }
                }
            };

            this.ExtraCleanupAction = async () =>
            {
                if (gameViewModel?.gameServiceClient is GameServiceClient serviceClient)
                {
                    serviceClient.StopConnectionMonitoring();
                }

                if (gameViewModel != null)
                {
                    await gameViewModel.CleanupBeforeClosingAsync();
                }

                if (chatViewModel != null)
                {
                    try { await chatViewModel.DisconnectAsync(); } catch { }
                }
            };
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
                gameViewModel.OpponentDinoHeadPlayed += OnOpponentDinoHeadPlayed;
                gameViewModel.OpponentBodyPartAttached += OnOpponentBodyPartAttached;
                gameViewModel.PlayerDinosClearedByElement += OnPlayerDinosClearedByElement;
                gameViewModel.DiscardPileUpdated += OnDiscardPileUpdated;
                gameViewModel.ArchArmyCleared += OnArchArmyCleared;
                MyDeckCanvas.SizeChanged += (s, args) => UpdatePlayerHandVisual();
                gameViewModel.GameEnded += OnGameEndedReceived;
                gameViewModel.PlayerLeftMatch += OnPlayerLeftMatchReceived;

                await gameViewModel.ConnectToGameAsync();

                if (gameViewModel.gameServiceClient is GameServiceClient serviceClient)
                {
                    serviceClient.StartConnectionMonitoring(timeoutSeconds: 7);
                }

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

        private void OnConnectionLost()
        {
            if (isHandlingDisconnection) return;
            isHandlingDisconnection = true;

            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Diagnostics.Debug.WriteLine("[DISCONNECT] ⚠️ Connection timeout - no server response");

                if (gameViewModel?.gameServiceClient is GameServiceClient serviceClient)
                {
                    serviceClient.StopConnectionMonitoring();
                }

                MessageBox.Show(
                    Lang.Match_ConnectionLostMessage ?? "La conexión con el servidor se ha perdido.\n\nLa partida será cerrada.",
                    Lang.Match_ConnectionLostTitle ?? "Conexión perdida",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                ForceExitToMainWindow();
            });
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

        private void InitializePlayersVisuals(List<LobbyPlayerDTO> players, string myUsername)
        {
            string myNickname = UserSession.Instance.GetNickname();
            var others = players.Where(player =>
                player.Username != myUsername &&
                player.Nickname != myNickname
            ).ToList();

            var me = players.FirstOrDefault(p => p.Username == myUsername || p.Nickname == myNickname);
            if (me != null)
            {
                playerPositionToUserId["P1"] = me.IdPlayer;
            }

            if (others.Count > 0)
            {
                Lb_TopPlayerName.Content = others[0].Nickname ?? others[0].Username;
                playerPositionToUserId["P2"] = others[0].IdPlayer;
                player2UserId = others[0].IdPlayer;
            }

            if (others.Count > 1)
            {
                Lb_LeftPlayerName.Content = others[1].Nickname ?? others[1].Username;
                Grid_LeftPlayer.Visibility = Visibility.Visible;
                playerPositionToUserId["P3"] = others[1].IdPlayer;
                player3UserId = others[1].IdPlayer;
            }
            else
            {
                Grid_LeftPlayer.Visibility = Visibility.Collapsed;
            }

            if (others.Count > 2)
            {
                Lb_RightPlayerName.Content = others[2].Nickname ?? others[2].Username;
                Grid_RightPlayer.Visibility = Visibility.Visible;
                playerPositionToUserId["P4"] = others[2].IdPlayer;
                player4UserId = others[2].IdPlayer;
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
                UpdatePlayerHandVisual();
                CheckDrawButtonState();

            });
        }

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
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.PropertyName == nameof(GameViewModel.RemainingMoves) ||
                    e.PropertyName == nameof(GameViewModel.IsMyTurn) ||
                    e.PropertyName == nameof(GameViewModel.RemainingCardsInDeck))
                {
                    CheckDrawButtonState();
                }
                else if (e.PropertyName == nameof(GameViewModel.SandArmyVisibility))
                {
                    if (Gr_SandArchs != null)
                        Gr_SandArchs.Visibility = gameViewModel.SandArmyVisibility;
                }
                else if (e.PropertyName == nameof(GameViewModel.WaterArmyVisibility))
                {
                    if (Gr_SeaArchs != null)
                        Gr_SeaArchs.Visibility = gameViewModel.WaterArmyVisibility;
                }
                else if (e.PropertyName == nameof(GameViewModel.WindArmyVisibility))
                {
                    if (Gr_WindArchs != null)
                        Gr_WindArchs.Visibility = gameViewModel.WindArmyVisibility;
                }
            });
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

        public void UpdatePlayerHandVisual()
        {
            if (MyDeckCanvas == null)
            {
                return;
            }

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

                System.Diagnostics.Debug.WriteLine($"[UI] Added card {cards[i].IdCard} at position {i}, left={leftPosition}");
            }
        }

        private void ShowArchPlacedAnimation(string armyName, int cardId)
        {
            HighlightArmyDeck(armyName);

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

        private CardCell GetCombinationCellForUserId(int userId, int cellIndex)
        {
            if (playerPositionToUserId.ContainsKey("P2") && playerPositionToUserId["P2"] == userId)
            {
                return TopPlayerCards.GetCombinationCell(cellIndex);
            }

            if (playerPositionToUserId.ContainsKey("P3") && playerPositionToUserId["P3"] == userId)
            {
                return LeftPlayerCell.GetCombinationCell(cellIndex);
            }

            if (playerPositionToUserId.ContainsKey("P4") && playerPositionToUserId["P4"] == userId)
            {
                return RightPlayerCards.GetCombinationCell(cellIndex);
            }

            return null;
        }

        private void OnOpponentDinoHeadPlayed(int userId, int dinoInstanceId, Card headCard)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Diagnostics.Debug.WriteLine($"[MAIN MATCH] Opponent {userId} played head in dino {dinoInstanceId}");

                CardCell targetCell = GetCombinationCellForUserId(userId, dinoInstanceId);

                if (targetCell != null)
                {
                    PlaceCardImageInGrid(targetCell, headCard);
                }
            });
        }

        private void OnOpponentBodyPartAttached(int userId, int dinoInstanceId, Card bodyCard)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Diagnostics.Debug.WriteLine($"[MAIN MATCH] Opponent {userId} attached {bodyCard.BodyPartType} to dino {dinoInstanceId}");

                CardCell targetCell = GetCombinationCellForUserId(userId, dinoInstanceId);

                if (targetCell != null)
                {
                    PlaceCardImageInGrid(targetCell, bodyCard);
                }
            });
        }

        private void HighlightArmyDeck(string armyName)
        {
            Grid targetGrid = null;

            switch (armyName)
            {
                case "Sand":
                    targetGrid = Gr_SandArchs;
                    break;
                case "Water":
                    targetGrid = Gr_SeaArchs;
                    break;
                case "Wind":
                    targetGrid = Gr_WindArchs;
                    break;
            }

            if (targetGrid == null) return;

            var glowEffect = new DropShadowEffect
            {
                Color = Colors.Gold,
                ShadowDepth = 0,
                BlurRadius = 40,
                Opacity = 1
            };

            targetGrid.Effect = glowEffect;

            var pulseAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.15,
                Duration = TimeSpan.FromSeconds(0.3),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3)
            };

            var scaleTransform = new ScaleTransform(1, 1, targetGrid.ActualWidth / 2, targetGrid.ActualHeight / 2);
            targetGrid.RenderTransform = scaleTransform;

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, pulseAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, pulseAnimation);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                targetGrid.Effect = null;
                targetGrid.RenderTransform = null;
            };
            timer.Start();
        }

        private void Click_BtnSeeDeckP1(object sender, RoutedEventArgs e)
        {
            if (playerPositionToUserId.ContainsKey("P1"))
            {
                gameViewModel.ShowPlayerDeck(playerPositionToUserId["P1"]);
            }
        }

        private void Click_BtnSeeDeckP2(object sender, RoutedEventArgs e)
        {
            if (playerPositionToUserId.ContainsKey("P2"))
            {
                gameViewModel.ShowPlayerDeck(playerPositionToUserId["P2"]);
            }
        }

        private void Click_BtnSeeDeckP3(object sender, RoutedEventArgs e)
        {
            if (playerPositionToUserId.ContainsKey("P3"))
            {
                gameViewModel.ShowPlayerDeck(playerPositionToUserId["P3"]);
            }
        }

        private void Click_BtnSeeDeckP4(object sender, RoutedEventArgs e)
        {
            if (playerPositionToUserId.ContainsKey("P4"))
            {
                gameViewModel.ShowPlayerDeck(playerPositionToUserId["P4"]);
            }
        }

        private async void Click_BtnEndTurn(object sender, RoutedEventArgs e)
        {
            await gameViewModel.EndTurnManuallyAsync();
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

        private void Click_BtnProvokeNow(object sender, RoutedEventArgs e)
        {
            if (!gameViewModel.IsMyTurn)
            {
                MessageBox.Show(Lang.Match_NotYourTurn);
                return;
            }

            ActivateProvokeMode();
        }

        private async void Click_BtnOpenDiscardPile(object sender, RoutedEventArgs e)
        {
            if (!gameViewModel.IsMyTurn)
            {
                MessageBox.Show(Lang.Match_NotYourTurn);
                return;
            }

            if (gameViewModel.RemainingMoves <= 0)
            {
                MessageBox.Show(Lang.Match_AlreadyUsedRolls);
                return;
            }

            var discardedCards = gameViewModel.BoardManager.DiscardPile.ToList();

            if (discardedCards.Count == 0)
            {
                MessageBox.Show(Lang.Match_DiscardPileEmpty);
                return;
            }

            var discardWindow = new MatchDiscardPile(discardedCards);
            bool? result = discardWindow.ShowDialog();

            if (result == true && discardWindow.SelectedCardId.HasValue)
            {
                int selectedCardId = discardWindow.SelectedCardId.Value;
                bool success = await gameViewModel.TakeCardFromDiscardPileAsync(selectedCardId);

                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"[MAIN MATCH] Successfully took card {selectedCardId} from discard pile");
                    UpdateArmyVisibility();
                }
            }
        }

        private async void Click_BtnOptions(object sender, RoutedEventArgs e)
        {
            var settings = new SettingsInMatch();
            settings.Owner = this;
            settings.ShowDialog();

            if (settings.RequestLeaveGame)
            {
                if (gameViewModel != null)
                {
                    gameViewModel.GameEnded -= OnGameEndedReceived;
                }

                if (chatViewModel != null)
                {
                    try 
                    {
                        await chatViewModel.DisconnectAsync();
                    } 
                    catch { }
                }

                await gameViewModel.LeaveGameAsync();
                NavigateToMainWindow();
            }
        }

        private void OnPlayerLeftMatchReceived(int userId)
        {
            if (player2UserId == userId)
            {
                TopPlayerCards.Visibility = Visibility.Hidden;
                Lb_TopPlayerName.Content = "";
            }
            else if (player3UserId == userId)
            {
                Grid_LeftPlayer.Visibility = Visibility.Hidden;
            }
            else if (player4UserId == userId)
            {
                Grid_RightPlayer.Visibility = Visibility.Hidden;
            }
        }

        private void UpdateArmyVisibility()
        {
            if (gameViewModel.BoardManager.SandArmy.Count > 0)
            {
                Gr_SandArchs.Visibility = Visibility.Visible;
            }
            else
            {
                Gr_SandArchs.Visibility = Visibility.Collapsed;
            }

            if (gameViewModel.BoardManager.WaterArmy.Count > 0)
            {
                Gr_SeaArchs.Visibility = Visibility.Visible;
            }
            else
            {
                Gr_SeaArchs.Visibility = Visibility.Collapsed;
            }

            if (gameViewModel.BoardManager.WindArmy.Count > 0)
            {
                Gr_WindArchs.Visibility = Visibility.Visible;
            }
            else
            {
                Gr_WindArchs.Visibility = Visibility.Collapsed;
            }

            System.Diagnostics.Debug.WriteLine($"[MAIN MATCH] Army visibility updated - Sand: {Gr_SandArchs.Visibility}, Water: {Gr_SeaArchs.Visibility}, Wind: {Gr_WindArchs.Visibility}");
        }

        private void ActivateProvokeMode()
        {

            if (gameViewModel.BoardManager.SandArmy.Count == 0 &&
                gameViewModel.BoardManager.WaterArmy.Count == 0 &&
                gameViewModel.BoardManager.WindArmy.Count == 0)
            {
                MessageBox.Show(Lang.Match_NoArmiesToProvoke); 
                return;
            }

            isProvokeModeActive = true;

            Btn_TakeACard.IsEnabled = false;
            Btn_EndTurn.IsEnabled = false;
            Btn_ProvokeNow.IsEnabled = false;

            for (int i = 1; i <= 6; i++)
            {
                var cell = BottomPlayerCards.GetCombinationCell(i);
                if (cell != null)
                {
                    cell.AllowDrop = false;
                }
            }

            DisablePlayerHandDragging();

            if (gameViewModel.BoardManager.SandArmy.Count > 0)
            {
                AddGlowToArmy(Gr_SandArchs);
                Gr_SandArchs.Cursor = Cursors.Hand;
                Gr_SandArchs.MouseLeftButtonDown += OnArmyClick;
            }

            if (gameViewModel.BoardManager.WaterArmy.Count > 0)
            {
                AddGlowToArmy(Gr_SeaArchs);
                Gr_SeaArchs.Cursor = Cursors.Hand;
                Gr_SeaArchs.MouseLeftButtonDown += OnArmyClick;
            }

            if (gameViewModel.BoardManager.WindArmy.Count > 0)
            {
                AddGlowToArmy(Gr_WindArchs);
                Gr_WindArchs.Cursor = Cursors.Hand;
                Gr_WindArchs.MouseLeftButtonDown += OnArmyClick;
            }

            System.Diagnostics.Debug.WriteLine("[PROVOKE] Attack mode activated - Click on an army");
        }

        private void DesactivateAttackMode()
        {
            isProvokeModeActive = false;

            RemoveGlowFromArmy(Gr_SandArchs);
            RemoveGlowFromArmy(Gr_SeaArchs);
            RemoveGlowFromArmy(Gr_WindArchs);

            Gr_SandArchs.Cursor = Cursors.Arrow;
            Gr_SeaArchs.Cursor = Cursors.Arrow;
            Gr_WindArchs.Cursor = Cursors.Arrow;

            Gr_SandArchs.MouseLeftButtonDown -= OnArmyClick;
            Gr_SeaArchs.MouseLeftButtonDown -= OnArmyClick;
            Gr_WindArchs.MouseLeftButtonDown -= OnArmyClick;

            CheckDrawButtonState(); 
            Btn_EndTurn.IsEnabled = gameViewModel.IsMyTurn;
            Btn_ProvokeNow.IsEnabled = gameViewModel.IsMyTurn;

            for (int i = 1; i <= 6; i++)
            {
                var cell = BottomPlayerCards.GetCombinationCell(i);
                if (cell != null)
                {
                    cell.AllowDrop = true;
                }
            }

            EnablePlayerHandDragging();

        }

        private void OnArmyClick(object sender, MouseButtonEventArgs e)
        {
            if (!isProvokeModeActive) return;

            ArmyType selectedArmy = ArmyType.Sand;

            if (sender == Gr_SandArchs)
                selectedArmy = ArmyType.Sand;
            else if (sender == Gr_SeaArchs)
                selectedArmy = ArmyType.Water;
            else if (sender == Gr_WindArchs)
                selectedArmy = ArmyType.Wind;

            System.Diagnostics.Debug.WriteLine($"[PROVOKE] Army selected: {selectedArmy}");

            DesactivateAttackMode();
            OpenProvokeWindow(selectedArmy);
        }

        private void AddGlowToArmy(Grid armyGrid)
        {
            armyGrid.Effect = new DropShadowEffect
            {
                Color = Colors.OrangeRed,
                ShadowDepth = 0,
                BlurRadius = 40,
                Opacity = 1
            };

            var pulseAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.1,
                Duration = TimeSpan.FromSeconds(0.5),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            var scaleTransform = new ScaleTransform(1, 1, armyGrid.ActualWidth / 2, armyGrid.ActualHeight / 2);
            armyGrid.RenderTransform = scaleTransform;

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, pulseAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, pulseAnimation);
        }

        private void RemoveGlowFromArmy(Grid armyGrid)
        {
            armyGrid.Effect = null;
            armyGrid.RenderTransform = null;
            armyGrid.BeginAnimation(Grid.OpacityProperty, null);
        }

        private void OpenProvokeWindow(ArmyType selectedArmy)
        {
            var playerNames = new Dictionary<int, string>();
            foreach (var player in playersInMatch)
            {
                playerNames[player.IdPlayer] = player.Nickname ?? player.Username;
            }

            var viewModel = new MatchProvokeViewModel(
                gameViewModel.BoardManager,
                playerNames,
                gameViewModel.DetermineMyUserId(),
                selectedArmy
            );

            var provokeWindow = new MatchProvoke.MatchProvoke(viewModel);
            bool? result = provokeWindow.ShowDialog();

            if (result == true)
            {
                ExecuteProvoke(selectedArmy);
            }
        }

        private async void ExecuteProvoke(ArmyType armyType)
        {
            try
            {
                int userId = gameViewModel.DetermineMyUserId();
                await gameViewModel.gameServiceClient.ProvokeArchArmyAsync(gameMatchCode, userId, armyType);

                System.Diagnostics.Debug.WriteLine($"[PROVOKE] Successfully provoked {armyType}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Lang.Match_ErrorProvokingArchs} {ex.Message}");
            }
        }

        private void OnPlayerDinosClearedByElement(int userId, ArmyType element)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                int myUserId = gameViewModel.DetermineMyUserId();

                if (userId == myUserId)
                {
                    ClearMyDinosByElement(element);
                }
                else
                {
                    ClearOpponentDinosByElement(userId, element);
                }

                System.Diagnostics.Debug.WriteLine($"[MAIN MATCH] Cleared {element} dinos for player {userId}");
            });
        }

        private void ClearMyDinosByElement(ArmyType element)
        {
            for (int i = 1; i <= 6; i++)
            {
                var cell = BottomPlayerCards.GetCombinationCell(i);
                if (cell != null && cell is CardCell cardCell)
                {
                    if (GetCellElement(cardCell) == element)
                    {
                        ClearCardCell(cardCell);
                        string cellId = $"IdCombinationCell_{i}";
                        gameViewModel.ActionManager.ClearSlot(cellId);
                    }
                }
            }
        }

        private void ClearOpponentDinosByElement(int userId, ArmyType element)
        {
            if (player2UserId == userId)
            {
                for (int i = 1; i <= 6; i++)
                {
                    var cell = TopPlayerCards.GetCombinationCell(i);
                    if (cell != null && cell is CardCell cardCell)
                    {
                        if (GetCellElement(cardCell) == element)
                        {
                            ClearCardCell(cardCell);
                        }
                    }
                }
            }
            else if (player3UserId == userId)
            {
                for (int i = 1; i <= 6; i++)
                {
                    var cell = LeftPlayerCell.GetCombinationCell(i);
                    if (cell != null && cell is CardCell cardCell)
                    {
                        if (GetCellElement(cardCell) == element)
                        {
                            ClearCardCell(cardCell);
                        }
                    }
                }
            }
            else if (player4UserId == userId)
            {
                for (int i = 1; i <= 6; i++)
                {
                    var cell = RightPlayerCards.GetCombinationCell(i);
                    if (cell != null && cell is CardCell cardCell)
                    {
                        if (GetCellElement(cardCell) == element)
                        {
                            ClearCardCell(cardCell);
                        }
                    }
                }
            }
        }

        private void ClearCardCell(CardCell cell)
        {
            var grayBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#222"));

            if (cell.Part_Head != null)
                cell.Part_Head.Background = grayBrush;

            if (cell.Part_Chest != null)
                cell.Part_Chest.Background = grayBrush;

            if (cell.Part_LeftArm != null)
                cell.Part_LeftArm.Background = grayBrush;

            if (cell.Part_RightArm != null)
                cell.Part_RightArm.Background = grayBrush;

            if (cell.Part_Legs != null)
                cell.Part_Legs.Background = grayBrush;
        }

        private ArmyType GetCellElement(CardCell cell)
        {
            if (cell.Part_Head?.Background is ImageBrush headBrush)
            {
                string imagePath = headBrush.ImageSource?.ToString();
                if (!string.IsNullOrEmpty(imagePath))
                {
                    var headCard = CardRepositoryModel.Cards.FirstOrDefault(c => c.CardRoute == imagePath);
                    if (headCard != null)
                    {
                        switch (headCard.Element)
                        {
                            case ElementType.Sand:
                                return ArmyType.Sand;
                            case ElementType.Water:
                                return ArmyType.Water;
                            case ElementType.Wind:
                                return ArmyType.Wind;
                        }
                    }
                }
            }

            return ArmyType.None;
        }

        private void OnDiscardPileUpdated()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Gr_DiscardedCards.Visibility = Visibility.Visible;
            });
        }

        private void OnArchArmyCleared(ArmyType armyType)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Grid targetGrid = null;

                switch (armyType)
                {
                    case ArmyType.Sand:
                        targetGrid = Gr_SandArchs;
                        break;
                    case ArmyType.Water:
                        targetGrid = Gr_SeaArchs;
                        break;
                    case ArmyType.Wind:
                        targetGrid = Gr_WindArchs;
                        break;
                }

                if (targetGrid != null)
                {
                    targetGrid.Visibility = Visibility.Collapsed;
                }
            });
        }

        private void DisablePlayerHandDragging()
        {
            foreach (UIElement child in MyDeckCanvas.Children)
            {
                if (child is DeckCardSelection cardControl)
                {
                    cardControl.IsEnabled = false;
                    cardControl.Opacity = 0.5; 
                }
            }
        }

        private void OnGameEndedReceived(string title, string message, GameEndedDTO gameData)
        {
            if (errorNotificationTimer != null)
            {
                errorNotificationTimer.Stop();
            }
            Gr_ErrorNotification.Visibility = Visibility.Collapsed;
            Gr_GameEndedOverlay.Visibility = Visibility.Visible;

            var endAnimationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };

            endAnimationTimer.Tick += (sender, args) =>
            {
                endAnimationTimer.Stop(); 
                ProcessPostGameNavigation(gameData);
            };

            endAnimationTimer.Start();
        }

        private void ProcessPostGameNavigation(GameEndedDTO gameData)
        {
            if (gameViewModel != null) gameViewModel.Dispose();
            if (chatViewModel != null) try { chatViewModel.Dispose(); } catch { }

            Gr_GameEndedOverlay.Visibility = Visibility.Collapsed;

            this.IsNavigating = true;

            if (gameData.Reason == "Aborted")
            {
                var mainWindow = new MainWindow();
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();
                this.Close();
                MessageBox.Show(Lang.Match_GameAbortedMessage, Lang.Match_GameOverTitle);
            }
            else
            {
                var statisticsWindow = new GameStatistics(gameData, this.playersInMatch);
                Application.Current.MainWindow = statisticsWindow;
                statisticsWindow.Show();
                this.Close();
            }
        }

        private void EnablePlayerHandDragging()
        {
            foreach (UIElement child in MyDeckCanvas.Children)
            {
                if (child is DeckCardSelection cardControl)
                {
                    cardControl.IsEnabled = true;
                    cardControl.Opacity = 1.0;
                }
            }
        }

        private void OnCriticalServiceError(string title, string message)
        {
            bool isFatalError = title.Contains("Conexión") ||
                                message.Contains("EndpointNotFound") ||
                                message.Contains("Faulted") ||
                                message.Contains("Timeout");

            if (isFatalError)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (gameViewModel == null) return;

                    MessageBox.Show(
                        $"{Lang.Match_ConnectionLostMessage}\n\nDetalle: {message}",
                        Lang.Match_ConnectionLostTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    ForceExitToMainWindow();
                });
            }
        }

        private void ForceExitToMainWindow()
        {
            try
            {
                if (gameViewModel?.gameServiceClient is GameServiceClient serviceClient)
                {
                    serviceClient.StopConnectionMonitoring();
                    serviceClient.ConnectionLost -= OnConnectionLost;
                    serviceClient.ServiceError -= OnCriticalServiceError;
                }

                if (gameViewModel != null)
                {
                    gameViewModel.Dispose();
                }

                if (chatViewModel != null)
                {
                    try { chatViewModel.Dispose(); } catch { }
                }

                var mainWindow = new MainWindow();
                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();
                this.Close();

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Critical error exiting match: {ex.Message}");
                Application.Current.Shutdown();
            }
        }

        private void NavigateToMainWindow()
        {
            if (gameViewModel != null) gameViewModel.Dispose();

            this.IsNavigating = true;

            var mainWindow = new MainWindow();
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
            this.Close();
        }

    }
}