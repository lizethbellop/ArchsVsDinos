using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.LobbyService;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
                lobbyServiceClient.ConnectToLobby(this.MatchCode, userAccount.Nickname);

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
            Application.Current.Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < 4; i++) Slots[i] = new SlotLobby { Username = "" };

                for (int i = 0; i < players.Count && i < 4; i++)
                {
                    UpdateSlot(i, players[i]);
                }
            });
        }

        private void UpdateSlot(int index, ArchsVsDinosClient.DTO.LobbyPlayerDTO player)
        {
            if (index < 0 || index >= Slots.Count) return;

            string currentNickname = UserSession.Instance.CurrentUser.Nickname;
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