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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
        private const int ChatContextMatch = 1;

        private const int ErrorNotificationSeconds = 3;
        private const double ErrorNotificationOpacity = 0.9;
        private const double ErrorFadeOutSeconds = 0.5;

        private const double DefaultHandCanvasWidth = 800;
        private const double HandCardWidth = 80;
        private const double HandCardOverlap = 50;
        private const double HandMinStartX = 10;

        private readonly int currentUserId;

        private readonly ChatViewModel chatViewModel;
        private readonly GameViewModel gameViewModel;
        private readonly string currentUsername;
        private readonly List<LobbyPlayerDTO> playersInMatch;
        private readonly string gameMatchCode;

        private readonly Dictionary<string, int> playerPositionToUserId = new Dictionary<string, int>();

        private DispatcherTimer errorNotificationTimer;
        private CardCell lastHoveredCardCell;

        private bool isProvokeModeActive = false;
        private bool isHandlingDisconnection = false;
        private bool isAttemptingReconnection = false;

        private bool closeCleanupExecuted = false;
        private bool isClosingHandled = false;

        private int player2UserId = 0;
        private int player3UserId = 0;
        private int player4UserId = 0;

        public MainMatch(List<LobbyPlayerDTO> players, string myUsername, string gameMatchCode, int myLobbyUserId)
        {
            InitializeComponent();

            currentUsername = myUsername;
            playersInMatch = players;
            this.gameMatchCode = gameMatchCode;
            currentUserId = myLobbyUserId;

            MusicPlayer.Instance.StopBackgroundMusic();
            MusicPlayer.Instance.PlayBackgroundMusic(MusicTracks.Match);

            try
            {
                chatViewModel = new ChatViewModel(new ChatServiceClient());
                Gr_Chat.DataContext = chatViewModel;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MATCH] Chat init error: {ex.Message}");
                MessageBox.Show(Lang.Match_ErrorChatNotAvailable);
            }

            try
            {
                IGameServiceClient gameService = new GameServiceClient();
                gameViewModel = new GameViewModel(gameService, this.gameMatchCode, currentUsername, players, myLobbyUserId);
                DataContext = gameViewModel;

                if (gameViewModel.gameServiceClient != null)
                {
                    gameViewModel.gameServiceClient.ServiceError += OnCriticalServiceError;
                    gameViewModel.gameServiceClient.ConnectionLost += OnConnectionLost;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MATCH] Game init error: {ex.Message}");
                MessageBox.Show(Lang.GlobalSystemError);
                Close();
                return;
            }

            InitializePlayersVisuals(playersInMatch, currentUsername);
            InitializeErrorTimer();
            InitializeDragAndDrop();

            gameViewModel.PropertyChanged += GameViewModelPropertyChanged;
            Loaded += MatchLoaded;

            ExtraCleanupAction = CleanupBeforeCloseAsync;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (IsNavigating || isClosingHandled)
            {
                base.OnClosing(e);
                return;
            }

            e.Cancel = true;
            isClosingHandled = true;

            Dispatcher.BeginInvoke(new Action(async () =>
            {
                try
                {
                    Func<Task> cleanup = ExtraCleanupAction;
                    ExtraCleanupAction = null;

                    if (cleanup != null)
                    {
                        await cleanup();
                    }
                    else
                    {
                        await CleanupBeforeCloseAsync();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MATCH] OnClosing cleanup error: {ex.Message}");
                }
                finally
                {
                    ForceLogoutOnClose = true;
                    IsNavigating = true;
                    Close();
                }
            }));

            base.OnClosing(e);
        }

        private async void MatchLoaded(object sender, RoutedEventArgs e)
        {
            await ConnectChatSafelyAsync();

            if (gameViewModel != null)
            {
                HookGameViewModelEvents();

                await ConnectGameSafelyAsync();

                if (gameViewModel.gameServiceClient is GameServiceClient serviceClient)
                {
                    serviceClient.StartConnectionMonitoring(timeoutSeconds: 7);
                }

                await Application.Current.Dispatcher.InvokeAsync(() => UpdatePlayerHandVisual(), DispatcherPriority.Loaded);
            }
        }

        private async Task ConnectChatSafelyAsync()
        {
            if (chatViewModel == null)
            {
                return;
            }

            try
            {
                await chatViewModel.ConnectAsync(
                    username: currentUsername,
                    userId: currentUserId,
                    context: ChatContextMatch,
                    matchCode: gameMatchCode
                );
            }
            catch (CommunicationException ex)
            {
                Debug.WriteLine($"[MATCH] Chat connect CommunicationException: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine($"[MATCH] Chat connect TimeoutException: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MATCH] Chat connect Exception: {ex.Message}");
            }
        }

        private async Task ConnectGameSafelyAsync()
        {
            try
            {
                await gameViewModel.ConnectToGameAsync();
            }
            catch (CommunicationException ex)
            {
                Debug.WriteLine($"[MATCH] ConnectToGame CommunicationException: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine($"[MATCH] ConnectToGame TimeoutException: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MATCH] ConnectToGame Exception: {ex.Message}");
            }
        }

        private void HookGameViewModelEvents()
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
            gameViewModel.GameConnectionLost += OnGameConnectionLostFromViewModel;
        }

        private void UnhookGameViewModelEvents()
        {
            if (gameViewModel == null)
            {
                return;
            }

            try
            {
                gameViewModel.GameEnded -= OnGameEndedReceived;
                gameViewModel.PlayerLeftMatch -= OnPlayerLeftMatchReceived;
                gameViewModel.GameConnectionLost -= OnGameConnectionLostFromViewModel;

                if (gameViewModel.BoardManager != null && gameViewModel.BoardManager.PlayerHand != null)
                {
                    gameViewModel.BoardManager.PlayerHand.CollectionChanged -= PlayerHandCollectionChanged;
                }

                gameViewModel.TurnChangedForUI -= UpdateTurnGlow;
                gameViewModel.ArchCardPlaced -= ShowArchPlacedAnimation;
                gameViewModel.OpponentDinoHeadPlayed -= OnOpponentDinoHeadPlayed;
                gameViewModel.OpponentBodyPartAttached -= OnOpponentBodyPartAttached;
                gameViewModel.PlayerDinosClearedByElement -= OnPlayerDinosClearedByElement;
                gameViewModel.DiscardPileUpdated -= OnDiscardPileUpdated;
                gameViewModel.ArchArmyCleared -= OnArchArmyCleared;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MATCH] Unhook events error: {ex.Message}");
            }
        }

        private async Task CleanupBeforeCloseAsync()
        {
            if (isAttemptingReconnection && gameViewModel?.gameServiceClient is GameServiceClient serviceClient)
            {
                serviceClient.CancelReconnectionAndExit();
            }

            if (closeCleanupExecuted)
            {
                return;
            }

            closeCleanupExecuted = true;

            StopMonitoringSafely();
            UnsubscribeServiceClientSafely();
            UnhookGameViewModelEvents();

            await LeaveGameSafelyAsync();
            await DisconnectChatSafelyAsync();

            DisposeSafely();
        }

        private void StopMonitoringSafely()
        {
            try
            {
                if (gameViewModel != null && gameViewModel.gameServiceClient is GameServiceClient serviceClient)
                {
                    serviceClient.StopConnectionMonitoring();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MATCH] StopConnectionMonitoring error: {ex.Message}");
            }
        }

        private void UnsubscribeServiceClientSafely()
        {
            try
            {
                if (gameViewModel != null && gameViewModel.gameServiceClient != null)
                {
                    gameViewModel.gameServiceClient.ConnectionLost -= OnConnectionLost;
                    gameViewModel.gameServiceClient.ServiceError -= OnCriticalServiceError;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MATCH] Unsubscribe service events error: {ex.Message}");
            }
        }

        private async Task LeaveGameSafelyAsync()
        {
            if (gameViewModel == null)
            {
                return;
            }

            try
            {
                Debug.WriteLine("[MATCH] Leaving game...");
                await gameViewModel.LeaveGameAsync();
                Debug.WriteLine("[MATCH] LeaveGameAsync completed.");
            }
            catch (CommunicationException ex)
            {
                Debug.WriteLine($"[MATCH] LeaveGameAsync CommunicationException: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine($"[MATCH] LeaveGameAsync TimeoutException: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MATCH] LeaveGameAsync Exception: {ex.Message}");
            }
        }

        private async Task DisconnectChatSafelyAsync()
        {
            if (chatViewModel == null)
            {
                return;
            }

            try
            {
                await chatViewModel.DisconnectAsync();
            }
            catch (CommunicationException ex)
            {
                Debug.WriteLine($"[MATCH] Chat Disconnect CommunicationException: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine($"[MATCH] Chat Disconnect TimeoutException: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MATCH] Chat Disconnect Exception: {ex.Message}");
            }
        }

        private void DisposeSafely()
        {
            try
            {
                if (errorNotificationTimer != null)
                {
                    errorNotificationTimer.Stop();
                    errorNotificationTimer = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MATCH] Timer dispose error: {ex.Message}");
            }

            try
            {
                if (chatViewModel != null)
                {
                    chatViewModel.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MATCH] Chat dispose error: {ex.Message}");
            }

            try
            {
                if (gameViewModel != null)
                {
                    gameViewModel.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MATCH] GameViewModel dispose error: {ex.Message}");
            }
        }

        private void OnConnectionLost()
        {
            if (isHandlingDisconnection || isAttemptingReconnection)
            {
                return;
            }

            isAttemptingReconnection = true;

            Application.Current.Dispatcher.Invoke(() =>
            {
                Debug.WriteLine("[MATCH] Connection timeout - attempting reconnection");

                var result = MessageBox.Show(
                    "Se perdió la conexión con el servidor. ¿Deseas intentar reconectar?",
                    "Conexión perdida",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.No)
                {
                    Debug.WriteLine("[MATCH] User declined reconnection");
                    isAttemptingReconnection = false;
                    isHandlingDisconnection = true;

                    StopMonitoringSafely();

                    MessageBox.Show(
                        "Saliendo de la partida...",
                        "Conexión perdida",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    ForceExitToLoginWindow();
                    return;
                }

                Debug.WriteLine("[MATCH] Starting reconnection attempts...");
                StartReconnectionProcess();
            });
        }

        private void OnCriticalServiceError(string title, string message)
        {
            bool isFatalError = (title != null && title.Contains("Conexión")) ||
                                (message != null && (message.Contains("EndpointNotFound") || message.Contains("Faulted") || message.Contains("Timeout")));

            if (!isFatalError)
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (gameViewModel == null)
                {
                    return;
                }

                MessageBox.Show(
                    $"{Lang.Match_ConnectionLostMessage}\n\nDetalle: {message}",
                    Lang.Match_ConnectionLostTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                ForceExitToLoginWindow();
            });
        }


        private void OnGameConnectionLostFromViewModel(string title, string message)
        {
            if (isHandlingDisconnection || isAttemptingReconnection)
            {
                return;
            }

            isHandlingDisconnection = true;

            Application.Current.Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"[MATCH] {title}: {message}");

                StopMonitoringSafely();

                MessageBox.Show(
                    message,
                    title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                ForceExitToLoginWindow();
            });
        }

        private void StartReconnectionProcess()
        {
            if (gameViewModel?.gameServiceClient is GameServiceClient serviceClient)
            {
                serviceClient.StopConnectionMonitoring();

                serviceClient.ReconnectionStarted += OnReconnectionStarted;
                serviceClient.ReconnectionCompleted += OnReconnectionCompleted;

                serviceClient.StartReconnectionAttempts();
            }
            else
            {
                Debug.WriteLine("[MATCH] Cannot start reconnection - service client not available");
                isAttemptingReconnection = false;
                isHandlingDisconnection = true;
                ForceExitToLoginWindow();
            }
        }

        private void OnReconnectionStarted()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Debug.WriteLine("[MATCH] Reconnection started notification");
            });
        }

        private void OnReconnectionCompleted(bool success)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                isAttemptingReconnection = false;

                if (success)
                {
                    Debug.WriteLine("[MATCH] ✅ Reconnection successful!");
                    isHandlingDisconnection = false;

                    MessageBox.Show(
                        "¡Reconexión exitosa! La partida continuará.",
                        "Conexión restaurada",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    Debug.WriteLine("[MATCH] ❌ Reconnection failed after all attempts");
                    isHandlingDisconnection = true;

                    MessageBox.Show(
                        "No se pudo reconectar después de varios intentos.",
                        "Conexión perdida",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );

                    ForceExitToLoginWindow();
                }

                if (gameViewModel?.gameServiceClient is GameServiceClient serviceClient)
                {
                    serviceClient.ReconnectionStarted -= OnReconnectionStarted;
                    serviceClient.ReconnectionCompleted -= OnReconnectionCompleted;
                }
            });
        }

        private void ForceExitToLoginWindow()
        {
            try
            {
                StopMonitoringSafely();
                UnsubscribeServiceClientSafely();
                UnhookGameViewModelEvents();

                ForceLogoutOnClose = true;
                IsNavigating = true;

                Window mainWindow = TryCreateMainWindow();
                if (mainWindow == null)
                {
                    Debug.WriteLine("[MATCH] Login window not found by reflection. Falling back to MainWindow.");
                    mainWindow = new MainWindow();
                }

                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MATCH] ForceExitToLoginWindow error: {ex.Message}");
                Application.Current.Shutdown();
            }
        }

        private void NavigateToMainWindow()
        {
            try
            {
                if (gameViewModel != null)
                {
                    gameViewModel.Dispose();
                }

                IsNavigating = true;

                Window mainWindow = TryCreateMainWindow();
                if (mainWindow == null)
                {
                    Debug.WriteLine("[MATCH] Login window not found by reflection. Falling back to MainWindow.");
                    mainWindow = new MainWindow();
                }

                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MATCH] NavigateToMainWindow error: {ex.Message}");
                Application.Current.Shutdown();
            }
        }

        private Window TryCreateMainWindow()
        {
            try
            {
                Assembly assembly = typeof(MainMatch).Assembly;
                Type mainType = FindMainWindowType(assembly);

                if (mainType == null)
                {
                    return null;
                }

                object instance = Activator.CreateInstance(mainType);
                return instance as Window;
            }
            catch (MissingMethodException ex)
            {
                Debug.WriteLine($"[MATCH] Login window ctor missing: {ex.Message}");
                return null;
            }
            catch (TargetInvocationException ex)
            {
                Debug.WriteLine($"[MATCH] Login window ctor threw: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MATCH] TryCreateMainWindow error: {ex.Message}");
                return null;
            }
        }

        private Type FindMainWindowType(Assembly assembly)
        {
            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray();
                Debug.WriteLine("[MATCH] ReflectionTypeLoadException while scanning window types.");
            }

            Type exactMainWindow = types.FirstOrDefault(t =>
                typeof(Window).IsAssignableFrom(t) &&
                string.Equals(t.Name, "MainWindow", StringComparison.Ordinal) &&
                t.GetConstructor(Type.EmptyTypes) != null);

            if (exactMainWindow != null)
            {
                return exactMainWindow;
            }

            Type exactMain = types.FirstOrDefault(t =>
                typeof(Window).IsAssignableFrom(t) &&
                string.Equals(t.Name, "MainWindow", StringComparison.Ordinal) &&
                t.GetConstructor(Type.EmptyTypes) != null);

            if (exactMain != null)
            {
                return exactMain;
            }

            Type firstMainLike = types.FirstOrDefault(t =>
                typeof(Window).IsAssignableFrom(t) &&
                t.Name.IndexOf("MainWindow", StringComparison.OrdinalIgnoreCase) >= 0 &&
                t.GetConstructor(Type.EmptyTypes) != null);

            return firstMainLike;
        }

        public void ManualDragOver(Point windowMousePosition, Card cardBeingDragged)
        {
            CardCell cardCellUnderMouse = FindCellUnderMouse(windowMousePosition);

            if (lastHoveredCardCell != null && lastHoveredCardCell != cardCellUnderMouse)
            {
                ClearAllCellEffects(lastHoveredCardCell);
            }

            lastHoveredCardCell = cardCellUnderMouse;

            if (cardCellUnderMouse == null)
            {
                return;
            }

            ClearAllCellEffects(cardCellUnderMouse);

            string logicError = gameViewModel.ActionManager.ValidateDrop(
                cardBeingDragged,
                cardCellUnderMouse.CellId,
                gameViewModel.RemainingMoves,
                gameViewModel.IsMyTurn
            );

            bool isLogicallyValid = logicError == null;
            int targetSubIndex = GetCorrectIndexForCard(cardBeingDragged);

            Color highlightColor = isLogicallyValid ? Colors.Lime : Colors.Red;

            if (targetSubIndex != -1)
            {
                ApplyNeonEffect(cardCellUnderMouse, targetSubIndex, highlightColor);
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

            if (targetCardCell == null)
            {
                return false;
            }

            string errorMessage = await gameViewModel.TryPlayCardAsync(cardBeingDropped, targetCardCell.CellId);

            if (errorMessage == null)
            {
                PlaceCardImageInGrid(targetCardCell, cardBeingDropped);
                Gr_ErrorNotification.Visibility = Visibility.Collapsed;
                return true;
            }

            ShowErrorNotification(errorMessage);
            return false;
        }

        private async void Click_BtnTakeACard(object sender, RoutedEventArgs e)
        {
            const int mainDrawPileIndex = 0;

            if (!(sender is Button button))
            {
                return;
            }

            button.IsEnabled = false;

            string errorMessage = await gameViewModel.ExecuteDrawCardFromView(mainDrawPileIndex);
            if (errorMessage != null)
            {
                ShowErrorNotification(errorMessage);
            }

            CheckDrawButtonState();
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
            Point screenMousePos = PointToScreen(windowMousePosition);

            for (int i = 1; i <= 6; i++)
            {
                CardCell cell = BottomPlayerCards.GetCombinationCell(i);
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

            Border targetBorder = cell.GetSubCell(index);
            if (targetBorder == null)
            {
                return;
            }

            targetBorder.Effect = new DropShadowEffect
            {
                Color = color,
                Direction = 0,
                ShadowDepth = 0,
                BlurRadius = 25,
                Opacity = 1
            };
        }

        private void ClearAllCellEffects(CardCell cell)
        {
            int[] validIndices = { 2, 4, 5, 6, 8 };

            foreach (int i in validIndices)
            {
                Border sub = cell.GetSubCell(i);
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

            if (targetBorder == null)
            {
                return;
            }

            try
            {
                targetBorder.Background = new ImageBrush(new BitmapImage(new Uri(card.CardRoute)));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MATCH] PlaceCardImageInGrid error: {ex.Message}");
            }
        }

        private void InitializePlayersVisuals(List<LobbyPlayerDTO> players, string myUsername)
        {
            string myNickname = UserSession.Instance.GetNickname();

            List<LobbyPlayerDTO> others = players
                .Where(player => player.Username != myUsername && player.Nickname != myNickname)
                .ToList();

            LobbyPlayerDTO me = players.FirstOrDefault(p => p.Username == myUsername || p.Nickname == myNickname);
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
            errorNotificationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(ErrorNotificationSeconds)
            };

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
                CardCell cell = BottomPlayerCards.GetCombinationCell(i);
                if (cell == null)
                {
                    continue;
                }

                cell.AllowDrop = true;
                cell.Drop += OnCardDrop;
                cell.DragOver += OnCardDragOver;
                cell.DragLeave += OnCardDragLeave;
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
            CardCell cell = sender as CardCell;
            if (cell == null)
            {
                return;
            }

            ClearAllCellEffects(cell);
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
            if (!(sender is CardCell cell) || !e.Data.GetDataPresent(typeof(Card)))
            {
                return;
            }

            ClearAllCellEffects(cell);

            Card card = (Card)e.Data.GetData(typeof(Card));
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

        private void GameViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.PropertyName == nameof(GameViewModel.RemainingMoves) ||
                    e.PropertyName == nameof(GameViewModel.IsMyTurn) ||
                    e.PropertyName == nameof(GameViewModel.RemainingCardsInDeck))
                {
                    CheckDrawButtonState();
                    return;
                }

                if (e.PropertyName == nameof(GameViewModel.SandArmyVisibility))
                {
                    if (Gr_SandArchs != null)
                    {
                        Gr_SandArchs.Visibility = gameViewModel.SandArmyVisibility;
                    }

                    return;
                }

                if (e.PropertyName == nameof(GameViewModel.WaterArmyVisibility))
                {
                    if (Gr_SeaArchs != null)
                    {
                        Gr_SeaArchs.Visibility = gameViewModel.WaterArmyVisibility;
                    }

                    return;
                }

                if (e.PropertyName == nameof(GameViewModel.WindArmyVisibility))
                {
                    if (Gr_WindArchs != null)
                    {
                        Gr_WindArchs.Visibility = gameViewModel.WindArmyVisibility;
                    }
                }
            });
        }

        private void CheckDrawButtonState()
        {
            Button drawButton = Gr_AllCards.FindName("Btn_TakeACard") as Button;
            if (drawButton == null)
            {
                return;
            }

            drawButton.Visibility = gameViewModel.RemainingCardsInDeck <= 0
                ? Visibility.Collapsed
                : Visibility.Visible;

            drawButton.IsEnabled = gameViewModel.IsMyTurn && gameViewModel.RemainingMoves > 0;
        }

        private void ShowErrorNotification(string message)
        {
            errorNotificationTimer.Stop();
            Gr_ErrorNotification.BeginAnimation(OpacityProperty, null);

            TxtB_ErrorNotificationContainer.Text = message;
            Gr_ErrorNotification.Visibility = Visibility.Visible;
            Gr_ErrorNotification.Opacity = ErrorNotificationOpacity;

            errorNotificationTimer.Start();
        }

        private void FadeOutErrorNotification()
        {
            var fadeOut = new DoubleAnimation
            {
                From = ErrorNotificationOpacity,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromSeconds(ErrorFadeOutSeconds))
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
                return;
            }

            if (Grid_LeftPlayer.Visibility == Visibility.Visible && IsPlayerName(Lb_LeftPlayerName, currentPlayerUsername))
            {
                HighlightPlayer(Lb_LeftPlayerName);
                return;
            }

            if (Grid_RightPlayer.Visibility == Visibility.Visible && IsPlayerName(Lb_RightPlayerName, currentPlayerUsername))
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

            double canvasWidth = MyDeckCanvas.ActualWidth > 0 ? MyDeckCanvas.ActualWidth : DefaultHandCanvasWidth;

            MyDeckCanvas.Children.Clear();

            List<Card> cards = gameViewModel.BoardManager.PlayerHand.ToList();
            if (cards.Count == 0)
            {
                return;
            }

            double totalWidth = (cards.Count - 1) * HandCardOverlap + HandCardWidth;
            double startX = (canvasWidth - totalWidth) / 2;

            if (startX < HandMinStartX)
            {
                startX = HandMinStartX;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                var cardControl = new DeckCardSelection { Card = cards[i] };
                double leftPosition = startX + (i * HandCardOverlap);

                cardControl.SetInitialPosition(leftPosition, -70);
                Panel.SetZIndex(cardControl, i);
                MyDeckCanvas.Children.Add(cardControl);

                Debug.WriteLine($"[MATCH UI] Added card {cards[i].IdCard} at position {i}, left={leftPosition}");
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

            Grid mainGrid = Content as Grid;
            if (mainGrid == null)
            {
                return;
            }

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
                Debug.WriteLine($"[MATCH] Opponent {userId} played head in dino {dinoInstanceId}");

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
                Debug.WriteLine($"[MATCH] Opponent {userId} attached {bodyCard.BodyPartType} to dino {dinoInstanceId}");

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

            if (targetGrid == null)
            {
                return;
            }

            targetGrid.Effect = new DropShadowEffect
            {
                Color = Colors.Gold,
                ShadowDepth = 0,
                BlurRadius = 40,
                Opacity = 1
            };

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

            List<Card> discardedCards = gameViewModel.BoardManager.DiscardPile.ToList();
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
                    Debug.WriteLine($"[MATCH] Took card {selectedCardId} from discard pile");
                    UpdateArmyVisibility();
                }
            }
        }

        private async void Click_BtnOptions(object sender, RoutedEventArgs e)
        {
            var settings = new SettingsInMatch();
            settings.Owner = this;
            settings.ShowDialog();

            if (!settings.RequestLeaveGame)
            {
                return;
            }

            try
            {
                if (gameViewModel != null)
                {
                    gameViewModel.GameEnded -= OnGameEndedReceived;
                }

                await DisconnectChatSafelyAsync();
                await LeaveGameSafelyAsync();

                NavigateToMainWindow();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MATCH] Options leave error: {ex.Message}");
                NavigateToMainWindow();
            }
        }

        private void OnPlayerLeftMatchReceived(int userId)
        {
            if (player2UserId == userId)
            {
                TopPlayerCards.Visibility = Visibility.Hidden;
                Lb_TopPlayerName.Content = "";
                return;
            }

            if (player3UserId == userId)
            {
                Grid_LeftPlayer.Visibility = Visibility.Hidden;
                return;
            }

            if (player4UserId == userId)
            {
                Grid_RightPlayer.Visibility = Visibility.Hidden;
            }
        }

        private void UpdateArmyVisibility()
        {
            Gr_SandArchs.Visibility = gameViewModel.BoardManager.SandArmy.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            Gr_SeaArchs.Visibility = gameViewModel.BoardManager.WaterArmy.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            Gr_WindArchs.Visibility = gameViewModel.BoardManager.WindArmy.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            Debug.WriteLine($"[MATCH] Army visibility - Sand:{Gr_SandArchs.Visibility}, Water:{Gr_SeaArchs.Visibility}, Wind:{Gr_WindArchs.Visibility}");
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
                CardCell cell = BottomPlayerCards.GetCombinationCell(i);
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

            Debug.WriteLine("[MATCH] Provoke mode activated.");
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
                CardCell cell = BottomPlayerCards.GetCombinationCell(i);
                if (cell != null)
                {
                    cell.AllowDrop = true;
                }
            }

            EnablePlayerHandDragging();
        }

        private void OnArmyClick(object sender, MouseButtonEventArgs e)
        {
            if (!isProvokeModeActive)
            {
                return;
            }

            ArmyType selectedArmy = ArmyType.Sand;

            if (sender == Gr_SandArchs)
            {
                selectedArmy = ArmyType.Sand;
            }
            else if (sender == Gr_SeaArchs)
            {
                selectedArmy = ArmyType.Water;
            }
            else if (sender == Gr_WindArchs)
            {
                selectedArmy = ArmyType.Wind;
            }

            Debug.WriteLine($"[MATCH] Provoke army selected: {selectedArmy}");

            DesactivateAttackMode();
            OpenProvokeWindow(selectedArmy);
        }

        private void AddGlowToArmy(Grid armyGrid)
        {
            if (armyGrid == null)
            {
                return;
            }

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
            if (armyGrid == null)
            {
                return;
            }

            armyGrid.Effect = null;
            armyGrid.RenderTransform = null;
            armyGrid.BeginAnimation(Grid.OpacityProperty, null);
        }

        private void OpenProvokeWindow(ArmyType selectedArmy)
        {
            var playerNames = new Dictionary<int, string>();
            foreach (LobbyPlayerDTO player in playersInMatch)
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

                Debug.WriteLine($"[MATCH] Provoked {armyType} successfully.");
            }
            catch (CommunicationException ex)
            {
                Debug.WriteLine($"[MATCH] Provoke CommunicationException: {ex.Message}");
                MessageBox.Show(Lang.Match_ErrorProvokingArchs);
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine($"[MATCH] Provoke TimeoutException: {ex.Message}");
                MessageBox.Show(Lang.Match_ErrorProvokingArchs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MATCH] Provoke Exception: {ex.Message}");
                MessageBox.Show(Lang.Match_ErrorProvokingArchs);
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

                Debug.WriteLine($"[MATCH] Cleared {element} dinos for player {userId}");
            });
        }

        private void ClearMyDinosByElement(ArmyType element)
        {
            for (int i = 1; i <= 6; i++)
            {
                CardCell cell = BottomPlayerCards.GetCombinationCell(i);
                if (cell == null)
                {
                    continue;
                }

                if (GetCellElement(cell) == element)
                {
                    ClearCardCell(cell);
                    string cellId = $"IdCombinationCell_{i}";
                    gameViewModel.ActionManager.ClearSlot(cellId);
                }
            }
        }

        private void ClearOpponentDinosByElement(int userId, ArmyType element)
        {
            if (player2UserId == userId)
            {
                ClearOpponentCellsByElement(TopPlayerCards, element);
                return;
            }

            if (player3UserId == userId)
            {
                ClearOpponentCellsByElement(LeftPlayerCell, element);
                return;
            }

            if (player4UserId == userId)
            {
                ClearOpponentCellsByElement(RightPlayerCards, element);
            }
        }

        private void ClearOpponentCellsByElement(dynamic playerCardsControl, ArmyType element)
        {
            for (int i = 1; i <= 6; i++)
            {
                CardCell cell = playerCardsControl.GetCombinationCell(i);
                if (cell == null)
                {
                    continue;
                }

                if (GetCellElement(cell) == element)
                {
                    ClearCardCell(cell);
                }
            }
        }

        private void ClearCardCell(CardCell cell)
        {
            var grayBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#222"));

            if (cell.Part_Head != null) cell.Part_Head.Background = grayBrush;
            if (cell.Part_Chest != null) cell.Part_Chest.Background = grayBrush;
            if (cell.Part_LeftArm != null) cell.Part_LeftArm.Background = grayBrush;
            if (cell.Part_RightArm != null) cell.Part_RightArm.Background = grayBrush;
            if (cell.Part_Legs != null) cell.Part_Legs.Background = grayBrush;
        }

        private ArmyType GetCellElement(CardCell cell)
        {
            ImageBrush headBrush = cell.Part_Head != null ? cell.Part_Head.Background as ImageBrush : null;
            if (headBrush == null)
            {
                return ArmyType.None;
            }

            string imagePath = headBrush.ImageSource != null ? headBrush.ImageSource.ToString() : null;
            if (string.IsNullOrEmpty(imagePath))
            {
                return ArmyType.None;
            }

            Card headCard = CardRepositoryModel.Cards.FirstOrDefault(c => c.CardRoute == imagePath);
            if (headCard == null)
            {
                return ArmyType.None;
            }

            switch (headCard.Element)
            {
                case ElementType.Sand:
                    return ArmyType.Sand;
                case ElementType.Water:
                    return ArmyType.Water;
                case ElementType.Wind:
                    return ArmyType.Wind;
                default:
                    return ArmyType.None;
            }
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
                DeckCardSelection cardControl = child as DeckCardSelection;
                if (cardControl != null)
                {
                    cardControl.IsEnabled = false;
                    cardControl.Opacity = 0.5;
                }
            }
        }

        private void EnablePlayerHandDragging()
        {
            foreach (UIElement child in MyDeckCanvas.Children)
            {
                DeckCardSelection cardControl = child as DeckCardSelection;
                if (cardControl != null)
                {
                    cardControl.IsEnabled = true;
                    cardControl.Opacity = 1.0;
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
            try
            {
                if (gameViewModel != null)
                {
                    gameViewModel.Dispose();
                }

                if (chatViewModel != null)
                {
                    chatViewModel.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MATCH] Dispose after game end error: {ex.Message}");
            }

            Gr_GameEndedOverlay.Visibility = Visibility.Collapsed;

            IsNavigating = true;

            if (gameData != null && gameData.Reason == "Aborted")
            {
                Window loginWindow = TryCreateMainWindow() ?? (Window)new MainWindow();
                Application.Current.MainWindow = loginWindow;
                loginWindow.Show();
                Close();

                MessageBox.Show(Lang.Match_GameAbortedMessage, Lang.Match_GameOverTitle);
                return;
            }

            try
            {
                var statisticsWindow = new GameStatistics(gameData, playersInMatch);
                Application.Current.MainWindow = statisticsWindow;
                statisticsWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MATCH] Open statistics error: {ex.Message}");
                NavigateToMainWindow();
            }
        }
    }
}