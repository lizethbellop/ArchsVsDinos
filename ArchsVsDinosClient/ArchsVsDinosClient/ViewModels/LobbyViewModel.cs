using ArchsVsDinosClient.LobbyService;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
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

        public LobbyViewModel()
        {
            lobbyServiceClient = new LobbyServiceClient();

            lobbyServiceClient.LobbyCreated += OnLobbyCreated;
            lobbyServiceClient.PlayerJoined += OnPlayerJoined;
            lobbyServiceClient.PlayerLeft += OnPlayerLeft;
            lobbyServiceClient.PlayerExpelled += OnPlayerExpelled;
            lobbyServiceClient.LobbyCancelled += OnLobbyCancelled;

            for (int i = 0; i < 4; i++)
                Slots.Add(new SlotLobby());

            if (!string.IsNullOrEmpty(UserSession.Instance.CurrentMatchCode))
            {
                MatchCode = UserSession.Instance.CurrentMatchCode;
            }

            var localPlayer = new LobbyPlayerDTO
            {
                Username = UserSession.Instance.CurrentUser.Username,
                Nickname = UserSession.Instance.CurrentUser.Nickname,
                IsHost = true
            };

            Players.Add(localPlayer);
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

        public void ExpelPlayer(string hostUsername, string targetUsername)
        {
            lobbyServiceClient.ExpelPlayer(hostUsername, targetUsername);
        }

        public void LeaveLobby(string username)
        {
            lobbyServiceClient.LeaveLobby(username);
        }

        public void CancellLobby(string matchCode, string usernameRequester)
        {
            lobbyServiceClient.CancellLobby(matchCode, usernameRequester);
        }
        public bool CurrentClientIsHost() => Players.FirstOrDefault(player => player.IsHost)?.Username == UserSession.Instance.CurrentUser.Username;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnLobbyCreated(LobbyPlayerDTO createdPlayer, string code)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UserSession.Instance.CurrentMatchCode = code;
                MatchCode = code;
                var existing = Players.FirstOrDefault(player => player.Username == createdPlayer.Username);
                if (existing == null)
                {
                    Players.Add(createdPlayer);
                }
                else
                {
                    existing.IsHost = createdPlayer.IsHost;
                }
                UpdateSlots();
                MatchCodeReceived?.Invoke(code);
            });

        }

        private void OnPlayerJoined(LobbyPlayerDTO joiningPlayer)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Players.All(player => player.Username != joiningPlayer.Username))
                    Players.Add(joiningPlayer);
                UpdateSlots();
            });
        }

        private void OnPlayerLeft(LobbyPlayerDTO leavingPlayer)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var existing = Players.FirstOrDefault(player => player.Username == leavingPlayer.Username);
                if (existing != null) Players.Remove(existing);
                UpdateSlots();
            });
        }

        private void OnLobbyCancelled(string code)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(Lang.Lobby_LobbyCancelled);
                NavigationUtils.GoToMainMenu();
            });
        }

        private void OnPlayerExpelled(LobbyPlayerDTO expelledPlayer)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var existing = Players.FirstOrDefault(player => player.Username == expelledPlayer.Username);
                if (existing != null) Players.Remove(existing);

                if (expelledPlayer.Username == UserSession.Instance.CurrentUser.Username)
                {
                    MessageBox.Show(Lang.Lobby_LobbyExpell);
                    NavigationUtils.GoToMainMenu();
                }
                UpdateSlots();
            });
        }

        private void UpdateSlots()
        {
            var localUsername = UserSession.Instance.CurrentUser.Username;

            var localPlayer = Players.FirstOrDefault(player => player.Username == localUsername);
            var otherPlayers = Players.Where(player => player.Username != localUsername).ToList();

            var orderedPlayers = new List<LobbyPlayerDTO>();
            if (localPlayer != null) orderedPlayers.Add(localPlayer);
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
                }
                else
                {
                    Slots[i].Username = string.Empty;
                    Slots[i].Nickname = string.Empty;
                    Slots[i].IsFriend = false;
                    Slots[i].CanKick = false;
                }
            }
        }
    }
}
