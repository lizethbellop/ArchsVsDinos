using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.LobbyService;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ArchsVsDinosClient.ViewModels
{
    public class LobbyViewModel : INotifyPropertyChanged
    {
        private readonly ILobbyServiceClient lobbyServiceClient;
        private readonly bool isHost;
        private string matchCode;

        public ObservableCollection<SlotLobby> Slots { get; set; }

        public string MatchCode
        {
            get => matchCode;
            set { matchCode = value; OnPropertyChanged(); }
        }

        public LobbyViewModel(bool isHost, ILobbyServiceClient client)
        {
            this.isHost = isHost;
            this.lobbyServiceClient = client;
            Slots = new ObservableCollection<SlotLobby>();
            InitializeSlots();
            this.MatchCode = UserSession.Instance.CurrentMatchCode;

            if (this.lobbyServiceClient != null)
            {
                this.lobbyServiceClient.PlayerListUpdated += OnPlayerListUpdated;
                this.lobbyServiceClient.GameStartedEvent += OnGameStarted;
            }
        }

        public async void InitializeLobby()
        {
            if (UserSession.Instance.CurrentUser == null) return;

            if (lobbyServiceClient == null)
            {
                MessageBox.Show("Error crítico: No hay conexión con el servicio del Lobby.");
                return;
            }

            var userAccount = new ArchsVsDinosClient.DTO.UserAccountDTO
            {
                Nickname = UserSession.Instance.CurrentUser.Nickname,
                IdPlayer = UserSession.Instance.CurrentPlayer?.IdPlayer ?? 0
            };

            MatchCreationResultCode result = await lobbyServiceClient.CreateLobbyAsync(userAccount);

            if (result == MatchCreationResultCode.MatchCreation_Success)
            {
                this.MatchCode = UserSession.Instance.CurrentMatchCode;

                UpdateSlot(0, new ArchsVsDinosClient.DTO.LobbyPlayerDTO
                {
                    Nickname = userAccount.Nickname,
                    IsHost = true,
                    IsReady = false
                });
            }
            else
            {
                string msg = LobbyResultCodeHelper.GetMessage(result);
                MessageBox.Show(msg);
            }
        }

        private void InitializeSlots()
        {
            for (int i = 0; i < 4; i++) Slots.Add(new SlotLobby { Username = "" });
        }

        private void OnPlayerListUpdated(List<ArchsVsDinosClient.DTO.LobbyPlayerDTO> players)
        {
            Debug.WriteLine($"[LOBBY] OnPlayerListUpdated called with {players.Count} players");
            foreach (var p in players)
            {
                Debug.WriteLine($"[LOBBY]   - {p.Nickname} (IsHost: {p.IsHost}, IsReady: {p.IsReady})");
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                string myNickname = UserSession.Instance.GetNickname();
                Debug.WriteLine($"[LOBBY] My nickname: {myNickname}");

                if (string.IsNullOrEmpty(myNickname) && players.Count > 0)
                {
                    myNickname = players.First().Nickname;
                    Debug.WriteLine($"[LOBBY] Using first player nickname: {myNickname}");
                }

                var orderedPlayers = new List<ArchsVsDinosClient.DTO.LobbyPlayerDTO>();
                var localPlayer = players.FirstOrDefault(p => p.Nickname == myNickname);

                if (localPlayer == null)
                {
                    Debug.WriteLine($"[LOBBY] Local player NOT found in list, creating placeholder");
                    localPlayer = new ArchsVsDinosClient.DTO.LobbyPlayerDTO
                    {
                        Nickname = myNickname,
                        IsReady = false,
                        IdPlayer = UserSession.Instance.GetPlayerId()
                    };
                }
                else
                {
                    Debug.WriteLine($"[LOBBY] Local player found in list");
                }

                orderedPlayers.Add(localPlayer);
                orderedPlayers.AddRange(players.Where(p => p.Nickname != myNickname));

                Debug.WriteLine($"[LOBBY] Final ordered list: {orderedPlayers.Count} players");

                for (int i = 0; i < 4; i++)
                {
                    Slots[i] = new SlotLobby { Username = "" };
                }

                for (int i = 0; i < orderedPlayers.Count && i < 4; i++)
                {
                    Debug.WriteLine($"[LOBBY] Updating slot {i} with {orderedPlayers[i].Nickname}");
                    UpdateSlot(i, orderedPlayers[i]);
                }
            });
        }

        private void UpdateSlot(int index, ArchsVsDinosClient.DTO.LobbyPlayerDTO player)
        {
            if (index < 0 || index >= Slots.Count) return;

            string currentNickname = UserSession.Instance.GetNickname();
            bool isMe = player.Nickname == currentNickname;

            Slots[index] = new SlotLobby
            {
                Username = player.Nickname,
                Nickname = player.Nickname,
                IsReady = player.IsReady,
                IsLocalPlayer = isMe,
                CanKick = this.isHost && !isMe
            };
        }


        public void StartTheGame(string matchCode, string username)
        {
            lobbyServiceClient.StartGame(matchCode);
        }

        /*public void CancellTheLobby(string matchCode, string username)
        {
            lobbyServiceClient.CancellLobby(matchCode, username);
        }*/

        public void LeaveOfTheLobby(string username)
        {
            lobbyServiceClient.LeaveLobby(username);
        }

        /*public void ExpelThePlayer(string targetUsername, string hostUsername)
        {
            lobbyServiceClient.ExpelPlayer(targetUsername, hostUsername);
        }*/

        public void InvitePlayerByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(MatchCode)) return;
            string sender = UserSession.Instance.CurrentUser.Username;
            _ = lobbyServiceClient.SendLobbyInviteByEmail(email, MatchCode, sender);
            MessageBox.Show(Lang.Lobby_EmailSended);
        }

        private void OnGameStarted(string matchCode)
        {
            Application.Current.Dispatcher.Invoke(() => MessageBox.Show("¡Juego Iniciado!"));
        }

        public bool CurrentClientIsHost() => isHost;

        public int GetPlayersCount() => Slots.Count(s => !string.IsNullOrEmpty(s.Username));

        public void SetWaitingForGuestCallback(bool waiting) { }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}