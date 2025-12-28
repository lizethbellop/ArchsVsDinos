using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.GameService; // DTOs de transporte (CardDrawnDTO, etc.)
using ArchsVsDinosClient.Models;      // Tus Modelos Locales (Card, CardCategory)
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient.ViewModels.GameViewsModels
{
    public class GameViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IGameServiceClient gameServiceClient;
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

        private Visibility waterArmyVisibility = Visibility.Visible;
        private Visibility sandArmyVisibility = Visibility.Visible;
        private Visibility windArmyVisibility = Visibility.Visible;

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<string> TurnChangedForUI;
        public event Action<string, int> ArchCardPlaced;

        public string MatchTimeDisplay => TimerManager.MatchTimeDisplay;

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

        public GameViewModel(IGameServiceClient gameServiceClient, string matchCode, string username, List<ArchsVsDinosClient.DTO.LobbyPlayerDTO> players, int myLobbyUserId = 0)
        {
            if (gameServiceClient == null)
            {
                throw new ArgumentNullException(nameof(gameServiceClient));
            }
            this.gameServiceClient = gameServiceClient;
            this.matchCode = matchCode;
            this.currentUsername = username;
            this.allPlayers = players ?? new List<ArchsVsDinosClient.DTO.LobbyPlayerDTO>();
            this.forcedUserId = myLobbyUserId;

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
            gameServiceClient.CardDrawn += OnCardDrawn;
            gameServiceClient.ArchAdded += OnArchAdded;
            gameServiceClient.ServiceError += OnServiceError;
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

            if (localError != null)
            {
                return localError;
            }

            int userId = DetermineMyUserId();

            try
            {
                if (card.Category == ArchsVsDinosClient.Models.CardCategory.DinoHead)
                {
                    await gameServiceClient.PlayDinoHeadAsync(matchCode, userId, card.IdCard);
                }
                else if (card.Category == ArchsVsDinosClient.Models.CardCategory.BodyPart)
                {
                    int headId = ActionManager.GetHeadIdFromCell(cellId);

                    var attachment = new AttachBodyPartDTO
                    {
                        CardId = card.IdCard,
                        DinoHeadCardId = headId
                    };

                    await gameServiceClient.AttachBodyPartAsync(matchCode, userId, attachment);
                }

                ActionManager.RegisterSuccessfulMove(card, cellId);
                BoardManager.PlayerHand.Remove(card);
                RemainingMoves--;

                if (RemainingMoves <= 0)
                {
                    EndTurnAutomatically();
                }

                return null;
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        public async Task<string> ExecuteDrawCardFromView(int pileIndex)
        {
            if (!IsMyTurn || RemainingMoves <= 0)
            {
                return Lang.Match_NotYourTurn;
            }

            try
            {
                int userId = DetermineMyUserId();
                await gameServiceClient.DrawCardAsync(matchCode, userId);
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
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
                await gameServiceClient.EndTurnAsync(matchCode, userId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al terminar turno: {ex.Message}");
            }
        }

        private void OnCardDrawn(CardDrawnDTO data)
        {
            Application.Current.Dispatcher.Invoke(() =>
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

                    if (newCard != null && newCard.Category != ArchsVsDinosClient.Models.CardCategory.Arch)
                    {
                        BoardManager.PlayerHand.Add(newCard);
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

        /*private void OnArchAdded(ArchAddedToBoardDTO data)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var archCard = CardRepositoryModel.GetById(data.ArchCard.IdCard);

                if (archCard == null) return;

                switch (data.ArchCard.Element)
                {
                    case GameService.ArmyType.Sand:
                        BoardManager.SandArmy.Add(archCard);
                        ArchCardPlaced?.Invoke("Sand", archCard.IdCard);
                        break;
                    case GameService.ArmyType.Water:
                        BoardManager.WaterArmy.Add(archCard);
                        ArchCardPlaced?.Invoke("Water", archCard.IdCard);
                        break;
                    case GameService.ArmyType.Wind:
                        BoardManager.WindArmy.Add(archCard);
                        ArchCardPlaced?.Invoke("Wind", archCard.IdCard);
                        break;
                }

                ArchCardPlaced?.Invoke(armyName, archCard.IdCard);
            });
        }*/

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
                        BoardManager.SandArmy.Add(archCard);
                        armyName = "Arena";
                        break;
                    case GameService.ArmyType.Water:
                        BoardManager.WaterArmy.Add(archCard);
                        armyName = "Agua";
                        break;
                    case GameService.ArmyType.Wind:
                        BoardManager.WindArmy.Add(archCard);
                        armyName = "Viento";
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

                RemainingCardsInDeck = data.DrawDeckCount;
                IsMyTurn = data.FirstPlayerUserId == myUserId;

                TimerManager.StartTimer(TimeSpan.FromMinutes(20));

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
                    MessageBox.Show($"{Lang.Match_InfoBegin2}{namePlayer}");
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
            Application.Current.Dispatcher.Invoke(() =>
            {
                TimerManager.UpdateTime(data.RemainingTime);

                int myUserId = DetermineMyUserId();
                if (data.PlayerScores != null && data.PlayerScores.ContainsKey(myUserId))
                {
                    CurrentPoints = data.PlayerScores[myUserId];
                }

                int currentPlayerId = data.CurrentPlayerUserId;
                IsMyTurn = (currentPlayerId == myUserId);

                if (IsMyTurn)
                {
                    RemainingMoves = 3;
                    MessageBox.Show(Lang.Match_YourTurn);
                }
                else
                {
                    RemainingMoves = 0;
                }

                string currentPlayerName = GetUsernameById(currentPlayerId);
                TurnChangedForUI?.Invoke(currentPlayerName);
            });
        }

        private void OnServiceError(string title, string msg)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        private string GetUsernameById(int userId)
        {
            var player = allPlayers.FirstOrDefault(p => p.IdPlayer == userId);
            if (player != null)
            {
                return player.Nickname;
            }
            return "Desconocido";
        }

        private int DetermineMyUserId()
        {
            if (this.forcedUserId != 0)
            {
                return this.forcedUserId;
            }

            var myPlayer = allPlayers.FirstOrDefault(p =>
                p.Username == currentUsername ||
                p.Nickname == currentUsername);

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

        public async void InitializeAndStartGameAsync()
        {
            await Task.CompletedTask;
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
                gameServiceClient.ServiceError -= OnServiceError;
            }
        }
    }
}