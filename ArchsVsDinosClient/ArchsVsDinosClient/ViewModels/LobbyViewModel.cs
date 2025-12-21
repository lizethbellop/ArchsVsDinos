using ArchsVsDinosClient.DTO;
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
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient.ViewModels
{
    public class LobbyViewModel : INotifyPropertyChanged
    {
        private readonly ILobbyServiceClient lobbyServiceClient;
        private readonly bool isHost;
        private string matchCode;
        public ChatViewModel Chat { get; }
        public event Action<string, string> LobbyConnectionLost;


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
                this.lobbyServiceClient.ConnectionError += OnLobbyConnectionError;
                this.lobbyServiceClient.PlayerKickedEvent += OnPlayerKicked;
            }

            Chat = new ChatViewModel(new ChatServiceClient());

            Chat.ChatDegraded += OnChatDegraded;
            Chat.RequestWindowClose += OnChatRequestWindowClose;
        }

        private void OnPlayerKicked(string nickname, string reason)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                string myNickname = UserSession.Instance.GetNickname();

                if (nickname == myNickname)
                {
                    // Fui expulsado
                    if (LobbyConnectionLost != null)
                    {
                        LobbyConnectionLost(
                            "Expulsado del lobby",
                            reason
                        );
                    }
                }
                else
                {
                    // Otro jugador fue expulsado
                    MessageBox.Show(
                        $"{nickname} fue expulsado del lobby.",
                        "Jugador expulsado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            });
        }

        private void OnChatDegraded(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    message + "\n\nEl lobby seguirá funcionando.",
                    "Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            });
        }

        private void OnChatRequestWindowClose(string title, string message)
        {
            // El chat decidió que es crítico, propagar al Lobby
            LobbyConnectionLost?.Invoke(title, message);
        }

        private void OnLobbyConnectionError(string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"[LOBBY VM] ConnectionError: {title} - {message}");

                if (LobbyConnectionLost != null)
                {
                    LobbyConnectionLost(
                        "Conexión perdida",
                        "Se perdió la conexión con el servidor. Serás redirigido al menú principal."
                    );
                }
            });
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

                if (result == MatchCreationResultCode.MatchCreation_Failure)
                {
                    if (LobbyConnectionLost != null)
                    {
                        LobbyConnectionLost(
                            "Error al crear lobby",
                            "No se pudo conectar con el servidor. Intenta nuevamente."
                        );
                    }
                }
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

        public async Task ConnectChatAsync()
        {
            try
            {
                await Chat.ConnectAsync(
                    UserSession.Instance.CurrentUser.Username,
                    context: 0,
                    matchCode: MatchCode
                );
            }
            catch (Exception ex)
            {
                OnChatDegraded($"No se pudo conectar al chat: {ex.Message}");
            }
        }

        public void Cleanup()
        {
            if (lobbyServiceClient != null)
            {
                lobbyServiceClient.ConnectionError -= OnLobbyConnectionError;
            }

            if (Chat != null)
            {
                Chat.ChatDegraded -= OnChatDegraded;
                Chat.RequestWindowClose -= OnChatRequestWindowClose;
            }
        }

        public void KickPlayer(string targetNickname)
        {
            if (!isHost) return;

            int hostUserId = UserSession.Instance.CurrentUser.IdUser;
            lobbyServiceClient.KickPlayer(MatchCode, hostUserId, targetNickname);
        }

    }
}