using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.GameService; 
using ArchsVsDinosClient.Models;     
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient.ViewModels.GameViewsModels
{
    public class GameViewModel : INotifyPropertyChanged, IDisposable
    {
        public readonly IGameServiceClient gameServiceClient;
        private readonly string matchCode;
        private readonly string currentUsername;
        private readonly List<ArchsVsDinosClient.DTO.LobbyPlayerDTO> allPlayers;
        private readonly int forcedUserId;

        public GameTimerManager TimerManager { get; }
        public GameBoardManager BoardManager { get; }
        public GameActionManager ActionManager { get; }

        private int remainingCardsInDeck;
        private bool isMyTurn;
        private int remainingMoves;
        private int currentPoints;
        private bool isGameStartedReceived;

        private Visibility waterArmyVisibility = Visibility.Collapsed;
        private Visibility sandArmyVisibility = Visibility.Collapsed;
        private Visibility windArmyVisibility = Visibility.Collapsed;

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<string> TurnChangedForUI;
        public event Action<string, int> ArchCardPlaced;
        public event Action<int, int, Card> OpponentDinoHeadPlayed;
        public event Action<int, int, Card> OpponentBodyPartAttached;
        public event Action<int, ArmyType> PlayerDinosClearedByElement;
        public event Action DiscardPileUpdated;
        public event Action<ArmyType> ArchArmyCleared;
        public event Action<string, string, GameEndedDTO> GameEnded;
        public event Action<int> PlayerLeftMatch;

        public string MatchTimeDisplay => TimerManager.MatchTimeDisplay;
        public string TurnTimeDisplay => TimerManager.TurnTimeDisplay;
        public System.Windows.Media.SolidColorBrush TurnTimeColor => TimerManager.TurnTimeColor;

        public int RemainingMoves
        {
            get => remainingMoves;
            set
            {
                remainingMoves = value;
                OnPropertyChanged(nameof(RemainingMoves));
            }
        }

        public int CurrentPoints
        {
            get => currentPoints;
            set
            {
                currentPoints = value;
                OnPropertyChanged(nameof(CurrentPoints));
            }
        }

        public int RemainingCardsInDeck
        {
            get => remainingCardsInDeck;
            set
            {
                remainingCardsInDeck = value;
                OnPropertyChanged(nameof(RemainingCardsInDeck));
            }
        }

        public bool IsMyTurn
        {
            get => isMyTurn;
            set
            {
                isMyTurn = value;
                OnPropertyChanged(nameof(IsMyTurn));
            }
        }

        public Visibility WaterArmyVisibility
        {
            get => waterArmyVisibility;
            set { waterArmyVisibility = value; OnPropertyChanged(nameof(WaterArmyVisibility)); }
        }

        public Visibility SandArmyVisibility
        {
            get => sandArmyVisibility;
            set { sandArmyVisibility = value; OnPropertyChanged(nameof(SandArmyVisibility)); }
        }

        public Visibility WindArmyVisibility
        {
            get => windArmyVisibility;
            set { windArmyVisibility = value; OnPropertyChanged(nameof(WindArmyVisibility)); }
        }

        public GameViewModel(IGameServiceClient gameServiceClient, string matchCode, string username, List<LobbyPlayerDTO> players, int myLobbyUserId = 0)
        {
            if (gameServiceClient == null)
            {
                throw new ArgumentNullException(nameof(gameServiceClient));
            }
            this.gameServiceClient = gameServiceClient;
            this.matchCode = matchCode;
            this.currentUsername = username;
            this.allPlayers = players ?? new List<LobbyPlayerDTO>();
            this.forcedUserId = myLobbyUserId;

            TimerManager = new GameTimerManager();
            BoardManager = new GameBoardManager();
            ActionManager = new GameActionManager();

            TimerManager.PropertyChanged += (sender, propertyChangedEventArgs) =>
            {
                if (propertyChangedEventArgs.PropertyName == nameof(GameTimerManager.MatchTimeDisplay))
                {
                    OnPropertyChanged(nameof(MatchTimeDisplay));
                }
                else if (propertyChangedEventArgs.PropertyName == nameof(GameTimerManager.TurnTimeDisplay))
                {
                    OnPropertyChanged(nameof(TurnTimeDisplay));
                }
                else if (propertyChangedEventArgs.PropertyName == nameof(GameTimerManager.TurnTimeColor))
                {
                    OnPropertyChanged(nameof(TurnTimeColor));
                }
            };

            SubscribeToGameEvents();
        }

        private void SubscribeToGameEvents()
        {
            gameServiceClient.GameInitialized += OnGameInitialized;
            gameServiceClient.GameStarted += OnGameStarted;
            gameServiceClient.TurnChanged += OnTurnChanged;
            gameServiceClient.CardDrawn += OnCardDrawn;
            gameServiceClient.ArchAdded += OnArchAdded;
            gameServiceClient.DinoHeadPlayed += OnDinoHeadPlayed;
            gameServiceClient.BodyPartAttached += OnBodyPartAttached;
            gameServiceClient.ServiceError += OnServiceError;
            gameServiceClient.ArchProvoked += OnArchProvoked;  
            gameServiceClient.ServiceError += OnServiceError;
            gameServiceClient.CardTakenFromDiscard += OnCardTakenFromDiscard;
            gameServiceClient.PlayerExpelled += OnPlayerExpelled;
            gameServiceClient.GameEnded += OnGameEnded;
        }

        public async Task ConnectToGameAsync()
        {
            int userId = DetermineMyUserId();
            Debug.WriteLine($"[GAME VM] Connecting to game with matchCode: {matchCode}, userId: {userId}");
            await gameServiceClient.ConnectToGameAsync(matchCode, userId);
        }

        public async Task<string> TryPlayCardAsync(Card card, string cellId)
        {
            string localError = ActionManager.ValidateDrop(card, cellId, RemainingMoves, IsMyTurn);
            if (localError != null) return localError;

            int userId = DetermineMyUserId();
            bool success = false;
            string serverErrorMessage = null;

            try
            {
                if (card.Category == CardCategory.DinoHead)
                {
                    var result = await gameServiceClient.PlayDinoHeadAsync(matchCode, userId, card.IdCard);

                    if (result == PlayCardResultCode.PlayCard_Success)
                    {
                        success = true;
                    }
                    else
                    {
                        serverErrorMessage = TranslatePlayCardError(result);
                    }
                }
                else if (card.Category == CardCategory.BodyPart)
                {
                    int headId = ActionManager.GetHeadIdFromCell(cellId);
                    var attachment = new AttachBodyPartDTO
                    {
                        CardId = card.IdCard,
                        DinoHeadCardId = headId
                    };

                    var result = await gameServiceClient.AttachBodyPartAsync(matchCode, userId, attachment);

                    if (result == PlayCardResultCode.PlayCard_Success)
                    {
                        success = true;
                    }
                    else
                    {
                        serverErrorMessage = TranslatePlayCardError(result);
                    }
                }

                if (success)
                {
                    ActionManager.RegisterSuccessfulMove(card, cellId);
                    BoardManager.PlayerHand.Remove(card);
                    RemainingMoves--;

                    if (RemainingMoves <= 0)
                    {
                        EndTurnAutomatically();
                    }
                    return null; 
                }
                else
                {
                    return serverErrorMessage ?? Lang.GlobalUnexpectedError;
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        public async Task<string> ExecuteDrawCardFromView(int pileIndex)
        {
            if (!IsMyTurn || RemainingMoves <= 0) return Lang.Match_NotYourTurn;

            try
            {
                int userId = DetermineMyUserId();
                var result = await gameServiceClient.DrawCardAsync(matchCode, userId);

                if (result == DrawCardResultCode.DrawCard_Success)
                {
                    return null;
                }
                else
                {
                    return TranslateDrawCardError(result);
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<bool> TakeCardFromDiscardPileAsync(int cardId)
        {
            if (!IsMyTurn || RemainingMoves <= 0) return false;

            try
            {
                int userId = DetermineMyUserId();
                var result = await gameServiceClient.TakeCardFromDiscardPileAsync(matchCode, userId, cardId);

                if (result == DrawCardResultCode.DrawCard_Success)
                {
                    System.Diagnostics.Debug.WriteLine($"[DISCARD PILE] Successfully took card {cardId}");
                    return true;
                }
                else
                {
                    MessageBox.Show(TranslateDrawCardError(result));
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Lang.Match_ErrorTakingACard} {ex.Message}");
                return false;
            }
        }

        public async Task EndTurnManuallyAsync()
        {
            if (!IsMyTurn)
            {
                MessageBox.Show(Lang.Match_NotYourTurn);
                return;
            }

            try
            {
                int userId = DetermineMyUserId();
                var result = await gameServiceClient.EndTurnAsync(matchCode, userId);

                if (result == EndTurnResultCode.EndTurn_DatabaseError)
                {
                    MessageBox.Show(Lang.GlobalDatabaseError);
                }
                else if (result != EndTurnResultCode.EndTurn_Success)
                {
                    MessageBox.Show(TranslateEndTurnError(result));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[END TURN MANUAL] Error: {ex.Message}");
                MessageBox.Show($"{Lang.Match_EndTurnError}: {ex.Message}");
            }
        }

        private string TranslatePlayCardError(PlayCardResultCode code)
        {
            switch (code)
            {
                case PlayCardResultCode.PlayCard_NotYourTurn: 
                    return Lang.Match_NotYourTurn;
                case PlayCardResultCode.PlayCard_CardNotInHand: 
                    return Lang.Match_CardNotFound;
                case PlayCardResultCode.PlayCard_InvalidDinoHead: 
                    return Lang.Match_InvalidCell;
                case PlayCardResultCode.PlayCard_MustAttachToHead: 
                    return Lang.Match_CellNeedDinoHead;
                case PlayCardResultCode.PlayCard_AlreadyPlayedTwoCards: 
                    return Lang.Match_AlreadyUsedRolls;
                default: 
                    return Lang.Match_CardCannotPlacedHere;
            }
        }

        private string TranslateDrawCardError(DrawCardResultCode code)
        {
            switch (code)
            {
                case DrawCardResultCode.DrawCard_NotYourTurn: 
                    return Lang.Match_NotYourTurn;
                case DrawCardResultCode.DrawCard_DrawPileEmpty: 
                    return Lang.Match_DeckEmpty;
                case DrawCardResultCode.DrawCard_AlreadyDrewThisTurn: 
                    return Lang.Match_AlreadyUsedRolls;
                default: 
                    return Lang.Match_ErrorDrawingCard;
            }
        }

        private string TranslateEndTurnError(EndTurnResultCode code)
        {
            switch (code)
            {
                case EndTurnResultCode.EndTurn_NotYourTurn: return Lang.Match_NotYourTurn;
                default: return Lang.Match_EndTurnError;
            }
        }

        public async Task LeaveGameAsync()
        {
            try
            {
                int userId = DetermineMyUserId();
                await gameServiceClient.LeaveGameAsync(matchCode, userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error leaving game: {ex.Message}");
                MessageBox.Show($"{Lang.Match_ErrorLeavingGame}: {ex.Message}");
            }
        }

        private void OnPlayerExpelled(PlayerExpelledDTO data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Diagnostics.Debug.WriteLine($"[PLAYER LEFT] User {data.ExpelledUsername} left.");

                if (data.RecycledCardIds != null && data.RecycledCardIds.Length > 0)
                {
                    foreach (var cardId in data.RecycledCardIds)
                    {
                        if (!BoardManager.DiscardPile.Any(card => card.IdCard == cardId))
                        {
                            var card = CardRepositoryModel.GetById(cardId);
                            if (card != null)
                            {
                                BoardManager.DiscardPile.Add(card);
                            }
                        }
                    }

                    DiscardPileUpdated?.Invoke();
                    System.Diagnostics.Debug.WriteLine($"[CLIENT] Added {data.RecycledCardIds.Length} recycled cards to discard pile.");
                }

                PlayerLeftMatch?.Invoke(data.ExpelledUserId);
            });
        }

        private void OnGameEnded(GameEndedDTO data)
        {
            if (gameServiceClient is GameServiceClient serviceClient)
            {
                serviceClient.StopConnectionMonitoring();
                System.Diagnostics.Debug.WriteLine("[GAME ENDED] Connection monitoring stopped");
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                string message = "";
                string title = Lang.Match_GameOverTitle;

                if (data.Reason == "Aborted")
                {
                    message = Lang.Match_GameAbortedMessage;
                }
                else if (data.Reason == "ArchsVictory")
                {
                    message = Lang.Match_DefeatedByArchs; 
                }
                else
                {
                    string winnerName = GetUsernameById(data.WinnerUserId);
                    message = $"{Lang.Match_WhoIsTheWinner} {winnerName}";
                }

                GameEnded?.Invoke(title, message, data);
            });
        }

        private void OnCardDrawn(CardDrawnDTO data)
        {
            System.Diagnostics.Debug.WriteLine($"[CARD DRAWN] Card ID: {data.Card.IdCard}");
            System.Diagnostics.Debug.WriteLine($"[CARD DRAWN] Player who drew: {data.PlayerUserId}");

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (RemainingCardsInDeck > 0)
                {
                    RemainingCardsInDeck--;
                }

                int userIdWhoDrew = data.PlayerUserId;
                int myUserId = DetermineMyUserId();

                if (userIdWhoDrew == myUserId)
                {

                    var newCard = CardRepositoryModel.GetById(data.Card.IdCard);

                    if (newCard != null && newCard.Category != CardCategory.Arch)
                    {
                        BoardManager.PlayerHand.Add(newCard);
                    }

                    if (IsMyTurn)
                    {
                        RemainingMoves--;
                        Debug.WriteLine($"[CARD DRAWN] Remaining moves: {RemainingMoves}");  // ← AGREGAR ESTE LOG


                        if (RemainingMoves <= 0)
                        {
                            Debug.WriteLine("[CARD DRAWN] Auto-ending turn");
                            EndTurnAutomatically();
                        }
                    }
                }

            }));
        }

        private void OnArchAdded(ArchAddedToBoardDTO data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var archCard = CardRepositoryModel.GetById(data.ArchCard.IdCard);

                if (archCard == null) return;

                string armyName = "";

                switch (data.ArchCard.Element)
                {
                    case GameService.ArmyType.Sand:
                        if (!BoardManager.SandArmy.Any(c => c.IdCard == archCard.IdCard))
                        {
                            BoardManager.SandArmy.Add(archCard);
                            System.Diagnostics.Debug.WriteLine($"[ARCH ADDED] Sand Arch {archCard.IdCard} added. Total: {BoardManager.SandArmy.Count}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[ARCH ADDED] ⚠️ Sand Arch {archCard.IdCard} already exists, skipping");
                        }

                        if (BoardManager.SandArmy.Count > 0)
                        {
                            SandArmyVisibility = Visibility.Visible;
                        }

                        armyName = "Sand";
                        break;

                    case GameService.ArmyType.Water:
                        if (!BoardManager.WaterArmy.Any(c => c.IdCard == archCard.IdCard))
                        {
                            BoardManager.WaterArmy.Add(archCard);
                            System.Diagnostics.Debug.WriteLine($"[ARCH ADDED] Water Arch {archCard.IdCard} added. Total: {BoardManager.WaterArmy.Count}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[ARCH ADDED] ⚠️ Water Arch {archCard.IdCard} already exists, skipping");
                        }

                        if (BoardManager.WaterArmy.Count > 0)
                        {
                            WaterArmyVisibility = Visibility.Visible;
                        }

                        armyName = "Water";
                        break;

                    case GameService.ArmyType.Wind:
                        if (!BoardManager.WindArmy.Any(c => c.IdCard == archCard.IdCard))
                        {
                            BoardManager.WindArmy.Add(archCard);
                            System.Diagnostics.Debug.WriteLine($"[ARCH ADDED] Wind Arch {archCard.IdCard} added. Total: {BoardManager.WindArmy.Count}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[ARCH ADDED] ⚠️ Wind Arch {archCard.IdCard} already exists, skipping");
                        }

                        if (BoardManager.WindArmy.Count > 0)
                        {
                            WindArmyVisibility = Visibility.Visible;
                        }

                        armyName = "Wind";
                        break;
                }

                ArchCardPlaced?.Invoke(armyName, archCard.IdCard);
            });
        }

        private async void EndTurnAutomatically()
        {
            try
            {
                int userId = DetermineMyUserId();
                await gameServiceClient.EndTurnAsync(matchCode, userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error auto-ending turn: {ex.Message}");
            }
        }

        private void OnGameInitialized(GameInitializedDTO data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                RemainingCardsInDeck = data.RemainingCardsInDeck;
            });
        }

        private void OnGameStarted(GameStartedDTO data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (isGameStartedReceived)
                {
                    return;
                }
                isGameStartedReceived = true;

                int myUserId = DetermineMyUserId();
                BoardManager.UpdatePlayerHand(data.PlayersHands.ToList(), myUserId);
                BoardManager.UpdateInitialBoard(data.InitialBoard);

                if (BoardManager.SandArmy.Count > 0)
                {
                    SandArmyVisibility = Visibility.Visible;
                    System.Diagnostics.Debug.WriteLine($"[GAME STARTED] Sand army visible ({BoardManager.SandArmy.Count} cards)");
                }

                if (BoardManager.WaterArmy.Count > 0)
                {
                    WaterArmyVisibility = Visibility.Visible;
                    System.Diagnostics.Debug.WriteLine($"[GAME STARTED] Water army visible ({BoardManager.WaterArmy.Count} cards)");
                }

                if (BoardManager.WindArmy.Count > 0)
                {
                    WindArmyVisibility = Visibility.Visible;
                    System.Diagnostics.Debug.WriteLine($"[GAME STARTED] Wind army visible ({BoardManager.WindArmy.Count} cards)");
                }

                RemainingCardsInDeck = data.DrawDeckCount;
                IsMyTurn = data.FirstPlayerUserId == myUserId;

                TimerManager.SetMatchEndTime(data.MatchEndTime);
                TimerManager.ResetTurnTimer(data.TurnEndTime);

                ShowInitialArchsSummary(data.InitialBoard);

                if (IsMyTurn)
                {
                    RemainingMoves = 3;
                    MessageBox.Show(Lang.Match_YourTurn);
                }
                else
                {
                    RemainingMoves = 0;
                    string namePlayer = GetUsernameById(data.FirstPlayerUserId);
                    MessageBox.Show($"{namePlayer } {Lang.Match_InfoBegin2}");
                }
            });
        }

        private void ShowInitialArchsSummary(GameService.CentralBoardDTO board)
        {
            int sandCount = board.SandArmyCount;
            int waterCount = board.WaterArmyCount;
            int windCount = board.WindArmyCount;
            int totalArchs = sandCount + waterCount + windCount;

            if (totalArchs > 0)
            {
                string message = $"{Lang.Match_InitialDistribution1} \n\n" +
                                $"{Lang.Match_InitialDistribution2}:\n" +
                                $"🏜️ {Lang.Match_InitialDistribution3} {sandCount}\n" +
                                $"🌊 {Lang.Match_InitialDistribution4} {waterCount}\n" +
                                $"💨 {Lang.Match_InitialDistribution5} {windCount}\n\n" +
                                $"Total: {totalArchs} Archs";

                MessageBox.Show(message);
            }
        }

        private void OnTurnChanged(TurnChangedDTO data)
        {
            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    Debug.WriteLine($"[TURN CHANGED] New current user: {data.CurrentPlayerUserId}");

                    TimerManager.ResetTurnTimer(data.TurnEndTime);
                    int myUserId = DetermineMyUserId();

                    if (data.PlayerScores != null && data.PlayerScores.ContainsKey(myUserId))
                        CurrentPoints = data.PlayerScores[myUserId];

                    IsMyTurn = (data.CurrentPlayerUserId == myUserId);
                    RemainingMoves = IsMyTurn ? 3 : 0;

                    TurnChangedForUI?.Invoke(GetUsernameById(data.CurrentPlayerUserId));
                }
                catch (Exception ex) { Debug.WriteLine($"Error UI Turn: {ex.Message}"); }
            }));
        }

        private void OnServiceError(string title, string msg)
        {
            if (Application.Current == null || Application.Current.Dispatcher == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    MessageBox.Show(msg ?? "Error desconocido", title ?? "Error de Servicio",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch { }
            });
        }

        public async Task CleanupBeforeClosingAsync()
        {
            Debug.WriteLine("[GAME VM] Executing cleanup before window closes...");
            await LeaveGameAsync(); 
            this.Dispose();
        }

        private string GetUsernameById(int userId)
        {
            var player = allPlayers.FirstOrDefault(playerSelected => playerSelected.IdPlayer == userId);
            if (player != null)
            {
                return player.Nickname;
            }
            return "Desconocido";
        }

        public int DetermineMyUserId()
        {
            if (this.forcedUserId != 0)
            {
                return this.forcedUserId;
            }

            var myPlayer = allPlayers.FirstOrDefault(player =>
                player.Username == currentUsername ||
                player.Nickname == currentUsername);

            if (myPlayer != null && myPlayer.IdPlayer != 0)
            {
                return myPlayer.IdPlayer; 
            }

            int playerId = UserSession.Instance.GetPlayerId();
            if (playerId != 0) return playerId;

            return UserSession.Instance.GetUserId();
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void OnDinoHeadPlayed(DinoPlayedDTO data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var headCard = CardRepositoryModel.GetById(data.HeadCard.IdCard);

                if (headCard == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DINO HEAD] ⚠️ Card {data.HeadCard.IdCard} not found in repository");
                    return;
                }

                BoardManager.RegisterDinoHeadPlayed(data.PlayerUserId, data.DinoInstanceId, headCard);

                int myUserId = DetermineMyUserId();

                if (data.PlayerUserId != myUserId)
                {
                    string playerName = GetUsernameById(data.PlayerUserId);
                    System.Diagnostics.Debug.WriteLine($"[DINO HEAD] {playerName} played head card {headCard.IdCard}");

                    OpponentDinoHeadPlayed?.Invoke(data.PlayerUserId, data.DinoInstanceId, headCard);
                }
            });
        }

        private void OnBodyPartAttached(BodyPartAttachedDTO data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var bodyCard = CardRepositoryModel.GetById(data.BodyCard.IdCard);

                if (bodyCard == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[BODY PART] ⚠️ Card {data.BodyCard.IdCard} not found in repository");
                    return;
                }

                BoardManager.RegisterBodyPartAttached(data.PlayerUserId, data.DinoInstanceId, bodyCard);

                int myUserId = DetermineMyUserId();

                if (data.PlayerUserId != myUserId)
                {
                    string playerName = GetUsernameById(data.PlayerUserId);
                    System.Diagnostics.Debug.WriteLine($"[BODY PART] {playerName} attached {bodyCard.BodyPartType} to dino {data.DinoInstanceId}");

                    OpponentBodyPartAttached?.Invoke(data.PlayerUserId, data.DinoInstanceId, bodyCard);
                }
            });
        }
    
        private void OnArchProvoked(ArchArmyProvokedDTO data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Diagnostics.Debug.WriteLine($"[ARCH PROVOKED] Army: {data.ArmyType}, Winner: {data.BattleResult?.WinnerUsername ?? "None"}");

                ActionManager.ClearSlotsByElement(data.ArmyType);

                int myUserId = DetermineMyUserId();
                if (data.ProvokerUserId == myUserId)
                {
                    RemainingMoves = 0;
                    System.Diagnostics.Debug.WriteLine("[PROVOKE] All my moves consumed locally");
                }

                if (data.DiscardedPlayerCardIds != null)
                {
                    foreach (var cardId in data.DiscardedPlayerCardIds)
                    {
                        var card = CardRepositoryModel.GetById(cardId);
                        if (card != null && !BoardManager.DiscardPile.Any(cardSelected => cardSelected.IdCard == cardId))
                        {
                            BoardManager.DiscardPile.Add(card);
                        }
                    }
                }

                if (data.BattleResult?.ArchCards != null)
                {
                    foreach (var cardDto in data.BattleResult.ArchCards)
                    {
                        var card = CardRepositoryModel.GetById(cardDto.IdCard);
                        if (card != null && !BoardManager.DiscardPile.Any(cardSelected => cardSelected.IdCard == cardDto.IdCard))
                        {
                            BoardManager.DiscardPile.Add(card);
                        }
                    }
                }

                foreach (var player in allPlayers)
                {
                    BoardManager.ClearPlayerDinosByElement(player.IdPlayer, data.ArmyType);
                    PlayerDinosClearedByElement?.Invoke(player.IdPlayer, data.ArmyType);
                }

                if (data.BattleResult != null &&
                    data.BattleResult.DinosWon &&
                    data.BattleResult.WinnerUserId.HasValue &&
                    data.BattleResult.WinnerUserId.Value == myUserId)
                {
                    CurrentPoints += data.BattleResult.PointsAwarded;
                }

                switch (data.ArmyType)
                {
                    case ArmyType.Sand:
                        BoardManager.SandArmy.Clear();
                        SandArmyVisibility = Visibility.Collapsed;
                        break;
                    case ArmyType.Water:
                        BoardManager.WaterArmy.Clear();
                        WaterArmyVisibility = Visibility.Collapsed;
                        break;
                    case ArmyType.Wind:
                        BoardManager.WindArmy.Clear();
                        WindArmyVisibility = Visibility.Collapsed;
                        break;
                }

                string resultMessage;
                string resultTitle;

                if (data.BattleResult != null && data.BattleResult.DinosWon)
                {
                    resultMessage = $"{Lang.Match_ProvokeVictoryMessagePlayer}{data.BattleResult.WinnerUsername} {Lang.Match_ProvokeVictoryMessagePlayer2} {data.BattleResult.PointsAwarded} {Lang.Match_ProvokeVictoryMessagePlayer3}";
                    resultTitle = Lang.Match_ProvokeVictoryTitle;
                }
                else
                {
                    resultMessage = $"{Lang.Match_ProvokeTotalDefeatMessage1}";
                    resultTitle = Lang.Match_ProvokeTotalDefeatTitle;
                }

                MessageBox.Show(resultMessage, resultTitle);
                DiscardPileUpdated?.Invoke();
                ArchArmyCleared?.Invoke(data.ArmyType);
            });
        }

        private void PopulateDiscardPileFromProvoke(ArchArmyProvokedDTO data)
        {
            if (data.BattleResult?.ArchCards != null)
            {
                foreach (var archCardDTO in data.BattleResult.ArchCards)
                {
                    var card = CardRepositoryModel.GetById(archCardDTO.IdCard);
                    if (card != null && !BoardManager.DiscardPile.Any(c => c.IdCard == card.IdCard))
                    {
                        BoardManager.DiscardPile.Add(card);
                        System.Diagnostics.Debug.WriteLine($"[DISCARD PILE] Added Arch {card.IdCard} to discard pile");
                    }
                }
            }

            if (data.BattleResult?.PlayerPowers != null)
            {
                int myUserId = DetermineMyUserId();

                foreach (var playerPower in data.BattleResult.PlayerPowers)
                {
                    int userId = playerPower.Key;

                    if (BoardManager.PlayerDecks.ContainsKey(userId))
                    {
                        var playerDinos = BoardManager.PlayerDecks[userId].Values.ToList();

                        foreach (var dino in playerDinos)
                        {
                            if (dino.Head != null && GetElementFromCard(dino.Head) == data.ArmyType)
                            {
                                if (dino.Head != null && !BoardManager.DiscardPile.Any(card => card.IdCard == dino.Head.IdCard))
                                {
                                    BoardManager.DiscardPile.Add(dino.Head);
                                }
                                if (dino.Chest != null && !BoardManager.DiscardPile.Any(card => card.IdCard == dino.Chest.IdCard))
                                {
                                    BoardManager.DiscardPile.Add(dino.Chest);
                                }
                                if (dino.LeftArm != null && !BoardManager.DiscardPile.Any(card => card.IdCard == dino.LeftArm.IdCard))
                                {
                                    BoardManager.DiscardPile.Add(dino.LeftArm);
                                }
                                if (dino.RightArm != null && !BoardManager.DiscardPile.Any(card => card.IdCard == dino.RightArm.IdCard))
                                {
                                    BoardManager.DiscardPile.Add(dino.RightArm);
                                }
                                if (dino.Legs != null && !BoardManager.DiscardPile.Any(card => card.IdCard == dino.Legs.IdCard))
                                {
                                    BoardManager.DiscardPile.Add(dino.Legs);
                                }
                            }
                        }
                    }
                }
            }
        }

        private ArmyType GetElementFromCard(Card card)
        {
            switch (card.Element)
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

        private void OnCardTakenFromDiscard(CardTakenFromDiscardDTO data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Diagnostics.Debug.WriteLine($"[CARD TAKEN] Player {data.PlayerUserId} took card {data.CardId}");

                int myUserId = DetermineMyUserId();

                var cardToRemove = BoardManager.DiscardPile.FirstOrDefault(cardSelected => cardSelected.IdCard == data.CardId);
                if (cardToRemove != null)
                {
                    BoardManager.DiscardPile.Remove(cardToRemove);
                }

                var card = CardRepositoryModel.GetById(data.CardId);
                if (card == null) return;

                if (card.Category == CardCategory.Arch)
                {
                    switch (card.Element)
                    {
                        case ElementType.Sand:
                            if (!BoardManager.SandArmy.Any(cardSelected => cardSelected.IdCard == card.IdCard))
                            {
                                BoardManager.SandArmy.Add(card);
                            }
                            SandArmyVisibility = Visibility.Visible;
                            break;
                        case ElementType.Water:
                            if (!BoardManager.WaterArmy.Any(cardSelected => cardSelected.IdCard == card.IdCard))
                            {
                                BoardManager.WaterArmy.Add(card);
                            }
                            WaterArmyVisibility = Visibility.Visible;
                            break;
                        case ElementType.Wind:
                            if (!BoardManager.WindArmy.Any(cardSelected => cardSelected.IdCard == card.IdCard))
                            {
                                BoardManager.WindArmy.Add(card);
                            }
                            WindArmyVisibility = Visibility.Visible;
                            break;
                    }
                    System.Diagnostics.Debug.WriteLine($"[VISUAL UPDATE] Arch {card.IdCard} added to board for everyone.");
                }

                if (data.PlayerUserId == myUserId)
                {
                    if (card.Category != ArchsVsDinosClient.Models.CardCategory.Arch)
                    {
                        BoardManager.PlayerHand.Add(card);
                        System.Diagnostics.Debug.WriteLine($"[MY HAND] Card {card.IdCard} added to my hand");
                    }

                    if (IsMyTurn)
                    {
                        RemainingMoves--;
                        if (RemainingMoves <= 0)
                        {
                            EndTurnAutomatically(); 
                        }
                    }
                }
            });
        }

        public void Dispose()
        {
            TimerManager.StopTimer();
            if (gameServiceClient != null)
            {
                gameServiceClient.GameInitialized -= OnGameInitialized;
                gameServiceClient.GameStarted -= OnGameStarted;
                gameServiceClient.TurnChanged -= OnTurnChanged;
                gameServiceClient.CardDrawn -= OnCardDrawn;
                gameServiceClient.ArchAdded -= OnArchAdded;
                gameServiceClient.DinoHeadPlayed -= OnDinoHeadPlayed;
                gameServiceClient.BodyPartAttached -= OnBodyPartAttached;
                gameServiceClient.ArchProvoked -= OnArchProvoked;
                gameServiceClient.CardTakenFromDiscard -= OnCardTakenFromDiscard;
                gameServiceClient.ServiceError -= OnServiceError;
                gameServiceClient.PlayerExpelled -= OnPlayerExpelled;
                gameServiceClient.GameEnded -= OnGameEnded;
            }
        }

        public void ShowPlayerDeck(int userId)
        {
            var playerInfo = allPlayers.FirstOrDefault(player => player.IdPlayer == userId);
            if (playerInfo == null)
            {
                MessageBox.Show(Lang.Match_PlayerNotFoundDeck);
                return;
            }

            var playerDeck = BoardManager.GetPlayerDeck(userId);

            int playerPoints = 0;
            if (userId == DetermineMyUserId())
            {
                playerPoints = CurrentPoints;
            }

            var viewModel = new GameSeeDeckViewModel();
            viewModel.LoadPlayerDeck(playerInfo.Nickname, playerPoints, playerDeck);

            var window = new Views.MatchViews.MatchSeeDeck.MatchSeeDeckHorizontal(viewModel);
            window.ShowDialog();
        }


    }


}