using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.GameService;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.ViewModels.GameViewsModels;
using ArchsVsDinosClient.Views.MatchViews;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input; 
using System.Windows.Threading;

namespace ArchsVsDinosClient.ViewModels.GameViewsModels
{
    public class GameViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IGameServiceClient gameServiceClient;
        private readonly int matchId;
        private readonly string currentUsername;
        private readonly List<LobbyPlayerDTO> allPlayers;

        public GameTimerManager TimerManager { get; }
        public GameBoardManager BoardManager { get; }
        public Dictionary<string, DinoBuilder> MyDinos { get; } = new Dictionary<string, DinoBuilder>();
        public GameActionManager ActionManager { get; }

        private int remainingCardsInDeck;
        private bool isMyTurn;
        private bool isInitializing = false;
        private bool isInitialized = false;
        private bool gameStartedProcessed = false;
        private int remainingMoves = 0;
        private int currentPoints = 0;

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<string> TurnChangedForUI;
        public string MatchTimeDisplay => TimerManager.MatchTimeDisplay;

        public int RemainingMoves
        {
            get => remainingMoves;
            set { remainingMoves = value; OnPropertyChanged(nameof(RemainingMoves)); }
        }

        public int CurrentPoints
        {
            get => currentPoints;
            set { currentPoints = value; OnPropertyChanged(nameof(CurrentPoints)); }
        }

        public int RemainingCardsInDeck
        {
            get => remainingCardsInDeck;
            set { remainingCardsInDeck = value; OnPropertyChanged(nameof(RemainingCardsInDeck)); }
        }

        public bool IsMyTurn
        {
            get => isMyTurn;
            set { isMyTurn = value; OnPropertyChanged(nameof(IsMyTurn)); }
        }

        public bool IsPlayer2Turn { get; set; }
        public bool IsPlayer3Turn { get; set; }
        public bool IsPlayer4Turn { get; set; }

        public GameViewModel(IGameServiceClient gameServiceClient, int matchId, string username, List<LobbyPlayerDTO> players)
        {
            this.gameServiceClient = gameServiceClient ?? throw new ArgumentNullException(nameof(gameServiceClient));
            this.matchId = matchId;
            this.currentUsername = username;
            this.allPlayers = players ?? new List<LobbyPlayerDTO>();

            TimerManager = new GameTimerManager();
            BoardManager = new GameBoardManager();
            ActionManager = new GameActionManager();

            TimerManager.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(GameTimerManager.MatchTimeDisplay))
                {
                    OnPropertyChanged(nameof(MatchTimeDisplay));
                }
            };

            SubscribeToGameEvents();
        }

        private void SubscribeToGameEvents()
        {
            gameServiceClient.GameInitialized += OnGameInitialized;
            gameServiceClient.GameStarted += OnGameStarted;
            gameServiceClient.TurnChanged += OnTurnChanged;
            gameServiceClient.ConnectionError += OnConnectionError;
            gameServiceClient.CardDrawn += OnCardDrawn; 
        }

        public async Task<string> ExecuteDrawCardFromView(int drawPileNumber)
        {
            if (!IsMyTurn || RemainingMoves <= 0)
            {
                return Lang.Match_NotYourTurn;
            }

            try
            {
                var userId = DetermineMyUserId();

                DrawCardResultCode result = await gameServiceClient.DrawCardAsync(matchId, userId, drawPileNumber);

                if (result != DrawCardResultCode.Success)
                {
                    return GetDrawCardErrorMessage(result);
                }
            }
            catch (TimeoutException)
            {
                return Lang.GlobalServerError;
            }
            catch (CommunicationException)
            {
                return Lang.GlobalServerError;
            }
            catch (Exception)
            {
                return Lang.GlobalServerError;
            }

            return null;
        }

        private void OnCardDrawn(CardDrawnDTO data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (RemainingCardsInDeck > 0)
                {
                    RemainingCardsInDeck--;
                }

                if (data.PlayerUsername == currentUsername)
                {
                    var cardIdFromServer = data.Card.IdCard;

                    var newCard = CardRepositoryModel.GetById(cardIdFromServer);

                    if (newCard != null && data.Card.Type != "arch")
                    {
                        BoardManager.PlayerHand.Add(newCard);
                    }

                    if (IsMyTurn)
                    {
                        RemainingMoves--;
                    }
                }

            });
        }

        public async Task<string> TryPlayCardAsync(Card card, string cellId)
        {
            string error = ActionManager.ValidateDrop(card, cellId, RemainingMoves, IsMyTurn);
            if (error != null)
            {
                return error;
            }

            bool success = false;
            try
            {
                await Task.Delay(100);
                success = true;
            }
            catch (Exception)
            {
                return Lang.GlobalServerError;
            }

            if (success)
            {
                ActionManager.RegisterSuccessfulMove(card, cellId);
                BoardManager.PlayerHand.Remove(card);
                RemainingMoves--;
                return null;
            }
            else
            {
                return "El servidor rechazó el movimiento.";
            }
        }

        private string GetDrawCardErrorMessage(DrawCardResultCode result)
        {
            switch (result)
            {
                case DrawCardResultCode.NotYourTurn: 
                    return Lang.Match_NotYourTurn;
                case DrawCardResultCode.DrawPileEmpty: 
                    return Lang.Match_DeckCardsEmpty;
                case DrawCardResultCode.AlreadyDrewThisTurn: 
                    return Lang.Match_AlreadyUsedRolls;
                default: return Lang.GlobalServerError;
            }
        }

        public async Task InitializeAndStartGameAsync()
        {
            if (isInitializing || isInitialized) return;
            isInitializing = true;
            try
            {
                await Task.Delay(500);
                await gameServiceClient.InitializeGameAsync(matchId);
                isInitialized = true;
            }
            catch (Exception)
            {
                MessageBox.Show(Lang.GlobalServerError);
                isInitializing = false;
            }
        }

        private void OnGameInitialized(GameInitializedDTO data)
        {
            Application.Current.Dispatcher.Invoke(() => RemainingCardsInDeck = data.RemainingCardsInDeck);
        }

        private void OnGameStarted(GameStartedDTO data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (gameStartedProcessed) return;
                gameStartedProcessed = true;

                int myUserId = DetermineMyUserId();

                BoardManager.UpdatePlayerHand(data.PlayersHands.ToList(), myUserId);
                BoardManager.UpdateInitialBoard(data.InitialBoard);

                RemainingCardsInDeck = data.DrawPile1Count + data.DrawPile2Count + data.DrawPile3Count;
                IsMyTurn = data.FirstPlayerUsername == currentUsername;

                TimerManager.StartTimer(TimeSpan.FromMinutes(20));

                if (IsMyTurn)
                {
                    RemainingMoves = 3;
                    MessageBox.Show(Lang.Match_YourTurn);
                }
                else
                {
                    RemainingMoves = 0;
                }

                MessageBox.Show($"{Lang.Match_InfoBegin1} {data.FirstPlayerUsername} {Lang.Match_InfoBegin2}\n\n🎴 Cartas: {BoardManager.PlayerHand.Count}");
            });
        }

        private void OnTurnChanged(TurnChangedDTO data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                TimerManager.UpdateTime(data.RemainingTime);

                int myUserId = DetermineMyUserId();
                if (data.PlayerScores != null && data.PlayerScores.ContainsKey(myUserId))
                {
                    CurrentPoints = data.PlayerScores[myUserId];
                }

                IsMyTurn = data.CurrentPlayerUsername == currentUsername;
                RemainingMoves = IsMyTurn ? 3 : 0;
                TurnChangedForUI?.Invoke(data.CurrentPlayerUsername);
            });
        }

        private int DetermineMyUserId()
        {
            var myPlayer = allPlayers?.FirstOrDefault(player => player.Username == currentUsername);
            int userId = myPlayer?.IdPlayer ?? 0;

            if (userId == 0 && !string.IsNullOrEmpty(currentUsername))
            {
                userId = currentUsername.GetHashCode();
            }
            return userId;
        }

        private void OnConnectionError(string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error));
        }

        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void Dispose()
        {
            TimerManager.StopTimer();
            if (gameServiceClient != null)
            {
                gameServiceClient.GameInitialized -= OnGameInitialized;
                gameServiceClient.GameStarted -= OnGameStarted;
                gameServiceClient.TurnChanged -= OnTurnChanged;
                gameServiceClient.ConnectionError -= OnConnectionError;
                gameServiceClient.CardDrawn -= OnCardDrawn;

                if (gameServiceClient is ICommunicationObject comm)
                {
                    try { comm.Close(); } catch { comm.Abort(); }
                }
                else
                {
                    gameServiceClient.Dispose();
                }
            }
        }
    }
}