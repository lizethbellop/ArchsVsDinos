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
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient.ViewModels
{
    public class LobbyViewModel : INotifyPropertyChanged
    {
        private readonly ILobbyServiceClient lobbyServiceClient;
        private bool isHost;
        private string matchCode;
        private int myCurrentLobbyId;
        public ChatViewModel Chat { get; }
        public FriendRequestViewModel Friends { get; private set; }
        public event Action<string, string> LobbyConnectionLost;

        public ObservableCollection<SlotLobby> Slots { get; set; }

        public string MatchCode
        {
            get => matchCode;
            set { matchCode = value; OnPropertyChanged(); }
        }

        public bool IsHost
        {
            get => isHost;
            set
            {
                if (isHost != value)
                {
                    isHost = value;
                    OnPropertyChanged();

                    foreach (var slot in Slots)
                    {
                        slot.CanKick = isHost && !slot.IsLocalPlayer && !string.IsNullOrEmpty(slot.Username);
                    }
                }
            }
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
                this.lobbyServiceClient.PlayerLeft += OnPlayerLeft;
            }

            this.lobbyServiceClient = client;
            string myUsername = UserSession.Instance.CurrentUser.Username;
            this.Friends = new FriendRequestViewModel(myUsername);
            this.Friends.Subscribe(myUsername);

            Chat = new ChatViewModel(new ChatServiceClient());

            Chat.ChatDegraded += OnChatDegraded;
            Chat.RequestWindowClose += OnChatRequestWindowClose;
        }

        private void OnFriendRequestSentResult(bool success)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (success)
                {
                    MessageBox.Show(Lang.FriendRequest_SentSuccess);
                }
                else
                {
                    MessageBox.Show(Lang.FriendRequest_SentError);
                }
            });
        }

        private void OnPlayerKicked(string nickname, string reason)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                string myNickname = UserSession.Instance.GetNickname();

                if (nickname == myNickname)
                {
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
                    MessageBox.Show(
                        $"{nickname} fue expulsado del lobby.",
                        "Jugador expulsado",
                        MessageBoxButton.OK
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

        public void SendFriendRequest(string targetUsername)
        {
            MessageBox.Show($"Intentando enviar a: '{targetUsername}'");
            if (string.IsNullOrEmpty(targetUsername)) return;

            string myUsername = UserSession.Instance.CurrentUser.Username;
            if (myUsername == targetUsername) return;

            if (Friends != null)
            {
                try
                {
                    try
                    {
                        Friends.Subscribe(myUsername);
                    }
                    catch { }

                    Friends.SendFriendRequest(myUsername, targetUsername);
                    MessageBox.Show(Lang.FriendRequest_SentSuccess);
                }
                catch (CommunicationException)
                {
                    MessageBox.Show(Lang.FriendRequest_SentError);
                }
                catch (TimeoutException)
                {
                    MessageBox.Show(Lang.FriendRequest_SentError);
                }
                catch (Exception)
                {
                    MessageBox.Show(Lang.FriendRequest_SentError);
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

                var meInList = players.FirstOrDefault(p => p.Nickname == myNickname);
                if (meInList != null)
                {
                    this.isHost = meInList.IsHost;
                    this.myCurrentLobbyId = meInList.IdPlayer;
                }

                var orderedPlayers = new List<ArchsVsDinosClient.DTO.LobbyPlayerDTO>();

                var localPlayerDto = meInList ?? new ArchsVsDinosClient.DTO.LobbyPlayerDTO
                {
                    Nickname = myNickname,
                    IsReady = false,
                    IdPlayer = UserSession.Instance.GetPlayerId(),
                    IsHost = false
                };

                orderedPlayers.Add(localPlayerDto);
                orderedPlayers.AddRange(players.Where(p => p.Nickname != myNickname));

                for (int i = 0; i < 4; i++)
                {
                    if (i >= Slots.Count) Slots.Add(new SlotLobby());

                    if (i < orderedPlayers.Count)
                    {
                        UpdateSlot(i, orderedPlayers[i]);
                    }
                    else
                    {
                        SlotLobby emptySlot = Slots[i];

                        emptySlot.Username = "";
                        emptySlot.Nickname = "";
                        emptySlot.IsReady = false;
                        emptySlot.IsLocalPlayer = false;
                        emptySlot.CanKick = false;
                        emptySlot.IsFriend = false;
                        emptySlot.ProfilePicture = null;
                    }
                }
            });
        }

        private void UpdateSlot(int index, ArchsVsDinosClient.DTO.LobbyPlayerDTO player)
        {
            if (index < 0 || index >= Slots.Count) return;

            SlotLobby currentSlot = Slots[index];
            string currentNickname = UserSession.Instance.GetNickname();
            bool isMe = player.Nickname == currentNickname;

            string finalUsername = player.Username;

            if (isMe)
            {
                finalUsername = UserSession.Instance.CurrentUser.Username;
            }
            else if (string.IsNullOrEmpty(finalUsername))
            {
                finalUsername = player.Nickname;
            }

            currentSlot.Username = finalUsername;
            currentSlot.Nickname = player.Nickname;
            currentSlot.IsReady = player.IsReady;
            currentSlot.IsLocalPlayer = isMe;
            currentSlot.CanKick = this.isHost && !isMe;
            currentSlot.IsGuest = player.IdPlayer <= 0;
            currentSlot.LocalUserIsGuest = UserSession.Instance.GetPlayerId() <= 0;
        }

        public void StartTheGame(string matchCode, string username)
        {
            lobbyServiceClient.StartGame(matchCode);
        }

        public void LeaveOfTheLobby(string nickname)
        {
            lobbyServiceClient.LeaveLobby(nickname);
        }

        public void InvitePlayerByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(MatchCode)) return;
            string sender = UserSession.Instance.CurrentUser.Username;
            _ = lobbyServiceClient.SendLobbyInviteByEmail(email, MatchCode, sender);
            MessageBox.Show(Lang.Lobby_EmailSended);
        }

        public event Action NavigateToGame;

        private void OnGameStarted(string matchCode)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                NavigateToGame?.Invoke();
            });
        }

        public List<ArchsVsDinosClient.DTO.LobbyPlayerDTO> GetCurrentPlayers()
        {
            var players = new List<ArchsVsDinosClient.DTO.LobbyPlayerDTO>();

            foreach (var slot in Slots)
            {
                if (!string.IsNullOrEmpty(slot.Nickname))
                {
                    players.Add(new ArchsVsDinosClient.DTO.LobbyPlayerDTO
                    {
                        Nickname = slot.Nickname,
                        Username = slot.Username,
                        IdPlayer = slot.IsGuest ? -1 : 0, 
                        IsHost = slot.IsLocalPlayer && this.isHost,
                        IsReady = slot.IsReady
                    });
                }
            }

            return players;
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

            if (Friends != null)
            {
                string myUsername = UserSession.Instance.CurrentUser?.Username;

                if (!string.IsNullOrEmpty(myUsername))
                {
                    Friends.Unsubscribe(myUsername);
                }

                Friends.Dispose();
            }
        }

        public void KickPlayer(string targetNickname)
        {
            if (!IsHost)
            {
                MessageBox.Show(Lang.Lobby_OnlyHostCanKick);
                return;
            }

            int hostUserId = this.myCurrentLobbyId;
            lobbyServiceClient.KickPlayer(MatchCode, hostUserId, targetNickname);
        }

        private void OnPlayerLeft(ArchsVsDinosClient.DTO.LobbyPlayerDTO playerDto)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"[LOBBY VM] Jugador salió: {playerDto.Nickname}");

                var slotToRemove = Slots.FirstOrDefault(s =>
                    string.Equals(s.Nickname?.Trim(), playerDto.Nickname?.Trim(), StringComparison.CurrentCultureIgnoreCase));

                if (slotToRemove != null)
                {
                    int index = Slots.IndexOf(slotToRemove);

                    if (index >= 0)
                    {
                        Slots[index] = new SlotLobby
                        {
                            Username = "",
                            Nickname = "",
                            IsReady = false
                        };
                    }
                }
            });
        }

    }
}