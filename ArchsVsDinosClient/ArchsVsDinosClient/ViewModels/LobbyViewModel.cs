using ArchsVsDinosClient.LobbyService;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using ArchsVsDinosClient.Views.LobbyViews;
using ArchsVsDinosClient.Views.MatchViews;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace ArchsVsDinosClient.ViewModels
{
    public class LobbyViewModel : INotifyPropertyChanged
    {
        private readonly ILobbyServiceClient lobbyServiceClient;
        private string matchCode;
        public ObservableCollection<LobbyPlayerDTO> Players { get; private set; } = new ObservableCollection<LobbyPlayerDTO>();
        public ObservableCollection<SlotLobby> Slots { get; private set; } = new ObservableCollection<SlotLobby>();

        public string MatchCode
        {
            get => matchCode;
            set
            {
                if (matchCode != value)
                {
                    matchCode = value;
                    OnPropertyChanged(nameof(MatchCode));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event Action<string> MatchCodeReceived;

        public LobbyViewModel() : this(false) { }

        public LobbyViewModel(bool isHost, ILobbyServiceClient existingClient = null)
        {
            lobbyServiceClient = existingClient ?? new LobbyServiceClient();

            lobbyServiceClient.LobbyCreated += OnLobbyCreated;
            lobbyServiceClient.PlayerJoined += OnPlayerJoined;
            lobbyServiceClient.PlayerLeft += OnPlayerLeft;
            lobbyServiceClient.PlayerExpelled += OnPlayerExpelled;
            lobbyServiceClient.LobbyCancelled += OnLobbyCancelled;
            lobbyServiceClient.GameStartedEvent += OnGameStarted;

            for (int i = 0; i < 4; i++)
                Slots.Add(new SlotLobby());

            if (isHost)
            {
                var localPlayer = new LobbyPlayerDTO
                {
                    Username = UserSession.Instance.CurrentUser.Username,
                    Nickname = UserSession.Instance.CurrentUser.Nickname,
                    IsHost = true
                };

                Players.Add(localPlayer);
            }

            UpdateSlots();

        }

        public void InitializeLobby()
        {
            var userAccount = new UserAccountDTO
            {
                Username = UserSession.Instance.CurrentUser.Username,
                Nickname = UserSession.Instance.CurrentUser.Nickname
            };

            lobbyServiceClient.CreateLobby(userAccount);
        }

        public void ExpelThePlayer(string targetUsername, string hostUsername)
        {
            lobbyServiceClient.ExpelPlayer(targetUsername, hostUsername);
        }

        public void LeaveOfTheLobby(string username)
        {
            lobbyServiceClient.LeaveLobby(username);
        }

        public void CancellTheLobby(string matchCode, string usernameRequester)
        {
            lobbyServiceClient.CancellLobby(matchCode, usernameRequester);
        }

        public void StartTheGame(string matchCode, string hostUsername)
        {
            lobbyServiceClient.StartGame(matchCode, hostUsername);
        }

        public bool CurrentClientIsHost() => Players.FirstOrDefault(player => player.IsHost)?.Username == UserSession.Instance.CurrentUser.Username;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public int GetPlayersCount()
        {
            return Players?.Count ?? 0;
        }

        private void OnLobbyCreated(LobbyPlayerDTO createdPlayer, string code)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UserSession.Instance.CurrentMatchCode = code;
                MatchCode = code;

                var existingPlayer = Players.FirstOrDefault(player => player.Username == createdPlayer.Username);
                if (existingPlayer == null)
                {
                    Players.Add(createdPlayer);
                }
                else
                {
                    existingPlayer.IsHost = createdPlayer.IsHost;
                }

                UpdateSlots();
                MatchCodeReceived?.Invoke(code);
            });
        }

        private void OnPlayerJoined(LobbyPlayerDTO joiningPlayer)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var existingPlayer = Players.FirstOrDefault(player => player.Username == joiningPlayer.Username);
                if (existingPlayer == null)
                {
                    Players.Add(joiningPlayer);
                    UpdateSlots();
                }
            });
        }

        private void OnPlayerLeft(LobbyPlayerDTO leavingPlayer)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var existing = Players.FirstOrDefault(player => player.Username == leavingPlayer.Username);
                if (existing != null)
                {
                    Players.Remove(existing);
                }
                UpdateSlots();
            });
        }

        private void OnLobbyCancelled(string code)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (!CurrentClientIsHost())
                {
                    MessageBox.Show(Lang.Lobby_LobbyCancelled);

                    var currentWindow = Application.Current.Windows
                        .OfType<Window>()
                        .FirstOrDefault(window => window is Views.LobbyViews.Lobby);

                    currentWindow?.Close();

                    NavigationUtils.GoToMainMenu();
                }
            });
        }

        private void OnPlayerExpelled(LobbyPlayerDTO expelledPlayer)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var existing = Players.FirstOrDefault(player => player.Username == expelledPlayer.Username);
                if (existing != null)
                {
                    Players.Remove(existing);
                }

                if (expelledPlayer.Username == UserSession.Instance.CurrentUser.Username)
                {
                    MessageBox.Show(Lang.Lobby_LobbyExpell);
                    NavigationUtils.GoToMainMenu();
                }
                UpdateSlots();
            });
        }

        private void OnGameStarted(string matchCode, List<LobbyPlayerDTO> players)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var myUsername = UserSession.Instance.CurrentUser.Username;
                var convertedPlayers = players.Select(player => new ArchsVsDinosClient.DTO.LobbyPlayerDTO
                {
                    Username = player.Username,
                    Nickname = player.Nickname,
                    IsHost = player.IsHost
                }).ToList();

                var match = new MainMatch(convertedPlayers, myUsername);
                match.Show();

                foreach (Window window in Application.Current.Windows)
                {
                    if (window is Lobby)
                    {
                        window.Close();
                        break;
                    }
                }
            });
        }

        private void UpdateSlots()
        {
            var localUsername = UserSession.Instance.CurrentUser.Username;

            var localPlayer = Players.FirstOrDefault(player => player.Username == localUsername);
            var otherPlayers = Players.Where(player => player.Username != localUsername).ToList();

            var orderedPlayers = new List<LobbyPlayerDTO>();
            if (localPlayer != null)
            {
                orderedPlayers.Add(localPlayer);
            }
            orderedPlayers.AddRange(otherPlayers);

            for (int i = 0; i < Slots.Count; i++)
            {
                if (i < orderedPlayers.Count)
                {
                    var player = orderedPlayers[i];
                    Slots[i].Username = player.Username;
                    Slots[i].Nickname = player.Nickname;
                    Slots[i].IsFriend = false;
                    Slots[i].CanKick = CurrentClientIsHost() && player.Username != localUsername;
                    Slots[i].IsLocalPlayer = player.Username == localUsername;
                    Slots[i].IsFriend = false;
                }
                else
                {
                    Slots[i].Username = string.Empty;
                    Slots[i].Nickname = string.Empty;
                    Slots[i].IsFriend = false;
                    Slots[i].CanKick = false;
                    Slots[i].IsLocalPlayer = false;
                }
            }
        }
    }
}
