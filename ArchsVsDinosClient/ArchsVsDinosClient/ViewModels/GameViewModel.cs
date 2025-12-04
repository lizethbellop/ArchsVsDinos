using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.GameService;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient.ViewModels
{
    public class GameViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IGameServiceClient gameServiceClient;
        private readonly int matchId;
        private readonly string currentUsername;
        private readonly List<LobbyPlayerDTO> allPlayers;

        private int remainingCardsInDeck;
        private bool isMyTurn;
        private bool isInitializing = false;
        private bool isInitialized = false;
        private bool gameStartedProcessed = false;

        public ObservableCollection<Card> PlayerHand { get; } = new ObservableCollection<Card>();
        public ObservableCollection<Card> SandArmy { get; } = new ObservableCollection<Card>();
        public ObservableCollection<Card> WaterArmy { get; } = new ObservableCollection<Card>();
        public ObservableCollection<Card> WindArmy { get; } = new ObservableCollection<Card>();

        public int RemainingCardsInDeck
        {
            get => remainingCardsInDeck;
            set
            {
                if (remainingCardsInDeck != value)
                {
                    remainingCardsInDeck = value;
                    OnPropertyChanged(nameof(RemainingCardsInDeck));
                }
            }
        }

        public bool IsMyTurn
        {
            get => isMyTurn;
            set
            {
                if (isMyTurn != value)
                {
                    isMyTurn = value;
                    OnPropertyChanged(nameof(IsMyTurn));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public GameViewModel(IGameServiceClient gameServiceClient, int matchId, string username, List<LobbyPlayerDTO> players)
        {
            this.gameServiceClient = gameServiceClient ?? throw new ArgumentNullException(nameof(gameServiceClient));
            this.matchId = matchId;
            this.currentUsername = username;
            this.allPlayers = players ?? new List<LobbyPlayerDTO>();

            SubscribeToGameEvents();
        }

        private void SubscribeToGameEvents()
        {
            gameServiceClient.GameInitialized += OnGameInitialized;
            gameServiceClient.GameStarted += OnGameStarted;
            gameServiceClient.ConnectionError += OnConnectionError;
            Console.WriteLine("[GameViewModel] ✅ Subscribed to game events");
        }

        public async Task InitializeAndStartGameAsync()
        {

            if (isInitializing || isInitialized)
            {
                Console.WriteLine("[GameViewModel] Already initializing or initialized, skipping...");
                return;
            }

            isInitializing = true;

            try
            {
                await gameServiceClient.InitializeGameAsync(matchId);
                await Task.Delay(1000);
                await gameServiceClient.StartGameAsync(matchId);
                isInitialized = true;
            }
            catch (TimeoutException)
            {
                MessageBox.Show(Lang.GlobalServerError);
                isInitializing = false;
            }
            catch (System.ServiceModel.CommunicationException)
            {
                MessageBox.Show(Lang.GlobalServerError);
                isInitializing = false;
            }
            catch (Exception)
            {
                MessageBox.Show(Lang.GlobalSystemError);
                isInitializing = false;
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
            System.Diagnostics.Debug.WriteLine("🎮 GAME STARTED CALLBACK!");

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (gameStartedProcessed)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ GameStarted already processed, ignoring...");
                    return;
                }

                gameStartedProcessed = true;

                var myPlayer = allPlayers?.FirstOrDefault(player => player.Username == currentUsername);
                int myUserId = myPlayer?.IdPlayer ?? 0;

                System.Diagnostics.Debug.WriteLine($"MyUserId={myUserId}, Username={currentUsername}");

                var myHand = data.PlayersHands?.FirstOrDefault(playerHand => playerHand.UserId == myUserId);

                if (myHand != null && myHand.Cards != null)
                {
                    PlayerHand.Clear();

                    foreach (var cardDTO in myHand.Cards)
                    {
                        var card = CardRepositoryModel.GetById(cardDTO.IdCard);
                        if (card != null)
                        {
                            PlayerHand.Add(card);
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"✅ CARDS ADDED: {PlayerHand.Count}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ NO HAY MANO!");
                }

                RemainingCardsInDeck = data.DrawPile1Count + data.DrawPile2Count + data.DrawPile3Count;
                IsMyTurn = data.FirstPlayerUsername == currentUsername;

                MessageBox.Show($"{Lang.Match_InfoBegin1} {data.FirstPlayerUsername} {Lang.Match_InfoBegin2}\n\n🎴 Cartas: {PlayerHand.Count}");
            });
        }

        private void OnConnectionError(string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            if (gameServiceClient != null)
            {
                gameServiceClient.GameInitialized -= OnGameInitialized;
                gameServiceClient.GameStarted -= OnGameStarted;
                gameServiceClient.ConnectionError -= OnConnectionError;

                gameServiceClient.Dispose();
            }
        }
    }
}