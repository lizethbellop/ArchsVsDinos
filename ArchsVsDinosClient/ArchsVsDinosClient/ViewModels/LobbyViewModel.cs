using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.LobbyService;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using ArchsVsDinosClient.Views.LobbyViews;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
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
        public int MyActualPlayerId { get; private set; }
        private int myCurrentLobbyId;
        public ChatViewModel Chat { get; }
        public FriendRequestViewModel Friends { get; private set; }
        public event Action<string, string> LobbyConnectionLost;
        private List<string> currentFriendsList;
        public event Action<string> NavigateToLobbyAsGuest;
        private System.Timers.Timer reconnectionTimer;
        private bool isAttemptingReconnection = false;
        private int reconnectionAttempts = 0;
        private int lostHandled = 0;
        private const int MAX_RECONNECTION_ATTEMPTS = 5;
        private const int RECONNECTION_INTERVAL_MS = 5000;
        private bool userRequestedExit = false;

        private bool isInitializing = false;

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
                this.lobbyServiceClient.LobbyInvitationReceived += OnLobbyInvitationReceived;
                this.lobbyServiceClient.ConnectionLost += OnConnectionTimerExpired;
            }

            string myUsername = UserSession.Instance.CurrentUser.Username;
            this.Friends = new FriendRequestViewModel(myUsername);
            this.Friends.SubscribeAsync(myUsername);

            Chat = new ChatViewModel(new ChatServiceClient());

            Chat.ChatDegraded += OnChatDegraded;
            Chat.RequestWindowClose += OnChatRequestWindowClose;
        }

        public async Task<List<string>> LoadFriendsAsync()
        {
            try
            {
                string myUsername = UserSession.Instance.CurrentUser.Username;
                var friendClient = new FriendServiceClient();
                var response = await friendClient.GetFriendsAsync(myUsername);

                if (response != null && response.Success && response.Friends != null)
                {
                    currentFriendsList = response.Friends.ToList();
                    RefreshFriendStatusInSlots();
                    return currentFriendsList;
                }

                return new List<string>();
            }
            catch (CommunicationException ex)
            {
                Debug.WriteLine($"[LOBBY] Communication error loading friends: {ex.Message}");
                return new List<string>();
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine($"[LOBBY] Timeout loading friends: {ex.Message}");
                return new List<string>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOBBY] Error loading friends: {ex.Message}");
                return new List<string>();
            }
        }

        private void OnConnectionTimerExpired()
        {
            if (System.Threading.Interlocked.Exchange(ref lostHandled, 1) == 1)
                return;

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (lobbyServiceClient is LobbyServiceClient serviceClient)
                {
                    serviceClient.StopConnectionMonitoring();
                    serviceClient.ForceAbort();
                }

                Cleanup();

                UserSession.Instance.CurrentMatchCode = string.Empty;
                LobbyConnectionLost?.Invoke(
                    "Conexión perdida",
                    "Se perdió tu conexión con el servidor. Regresando al inicio de sesión."
                );
            }));
        }

        private void RefreshFriendStatusInSlots()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var slot in Slots)
                {
                    if (!string.IsNullOrEmpty(slot.Username) && !slot.IsLocalPlayer)
                    {
                        slot.IsFriend = currentFriendsList != null && currentFriendsList.Contains(slot.Username);
                    }
                }
            });
        }

        private void OnPlayerKicked(string nickname, string reason)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                string myNickname = UserSession.Instance.GetNickname();

                if (nickname == "SYSTEM" || nickname == myNickname)
                {
                    MessageBox.Show(reason, "Aviso de la Sala", MessageBoxButton.OK);
                    Cleanup();

                    MainWindow mainMenu = new MainWindow();

                    var lobbyWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is Lobby);

                    Application.Current.MainWindow = mainMenu;
                    mainMenu.Show();

                    lobbyWindow?.Close();
                }
                else
                {
                    Debug.WriteLine($"[LOBBY] The player {nickname} has been kicked from the lobby.");
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

                if (isInitializing)
                {
                    return;
                }

                if (!string.IsNullOrEmpty(MatchCode) && !isAttemptingReconnection)
                {
                    Debug.WriteLine($"[LOBBY VM] Active lobby detected, trying automatic reconnection...");
                    StartReconnectionAttempts();
                }
                else if (LobbyConnectionLost != null && !isAttemptingReconnection)
                {
                    LobbyConnectionLost(
                        "Connection lost",
                        "Se perdió la conexión con el servidor. Serás redirigido al menú principal."
                    );
                }
            });
        }

        public async Task<bool> InitializeLobbyAsync()
        {
            if (UserSession.Instance.CurrentUser == null)
                return false;

            if (lobbyServiceClient == null)
                return false;

            isInitializing = true;

            var userAccount = new ArchsVsDinosClient.DTO.UserAccountDTO
            {
                Nickname = UserSession.Instance.CurrentUser.Nickname,
                IdPlayer = UserSession.Instance.CurrentUser.IdUser
            };

            try
            {
                MatchCreationResultCode result =
                    await lobbyServiceClient.CreateLobbyAsync(userAccount);

                if (result == MatchCreationResultCode.MatchCreation_Success)
                {
                    MatchCode = UserSession.Instance.CurrentMatchCode;

                    UpdateSlot(0, new ArchsVsDinosClient.DTO.LobbyPlayerDTO
                    {
                        Nickname = userAccount.Nickname,
                        IsHost = true,
                        IsReady = false
                    });

                    if (lobbyServiceClient is LobbyServiceClient serviceClient)
                    {
                        serviceClient.StartConnectionMonitoring(timeoutSeconds: 12);
                    }

                    await ConnectChatAsync();
                    return true;
                }

                LobbyConnectionLost?.Invoke(
                    "Error al crear lobby",
                    LobbyResultCodeHelper.GetMessage(result)
                );

                return false;
            }
            catch
            {
                LobbyConnectionLost?.Invoke(
                    Lang.WcfNoConnection,
                    Lang.GlobalServerError
                );
                return false;
            }
            finally
            {
                isInitializing = false;
            }
        }


        public void SendFriendRequest(string targetUsername)
        {
            string myUsername = UserSession.Instance.CurrentUser.Username;
            if (myUsername == targetUsername) return;

            if (Friends != null)
            {
                try
                {
                    try
                    {
                        Friends.SubscribeAsync(myUsername);
                    }
                    catch { }

                    Friends.SendFriendRequestAsync(myUsername, targetUsername);
                    
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
                }

                var meInList = players.FirstOrDefault(p => p.Nickname == myNickname);
                if (meInList != null)
                {
                    this.isHost = meInList.IsHost;
                    this.MyActualPlayerId = meInList.IdPlayer;
                    this.myCurrentLobbyId = meInList.IdPlayer;

                    if (UserSession.Instance.GetPlayerId() == 0 && meInList.IdPlayer != 0)
                    {
                        if (UserSession.Instance.CurrentUser != null)
                        {
                            UserSession.Instance.CurrentUser.IdUser = meInList.IdPlayer;
                        }

                        if (UserSession.Instance.CurrentPlayer != null)
                        {
                            UserSession.Instance.CurrentPlayer.IdPlayer = meInList.IdPlayer;
                        }
                    }
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
                    if (i >= Slots.Count)
                    {
                        Slots.Add(new SlotLobby());
                    }

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

        private void UpdateSlot(int index, DTO.LobbyPlayerDTO player)
        {
            if (index < 0 || index >= Slots.Count)
            {
                return;
            }

            SlotLobby currentSlot = Slots[index];
            string currentNickname = UserSession.Instance.GetNickname();
            bool isMe = player.Nickname == currentNickname;

            string officialUsername = player.Username;

            if (isMe)
            {
                officialUsername = UserSession.Instance.CurrentUser.Username;
            }
            else if (string.IsNullOrEmpty(officialUsername))
            {
                officialUsername = player.Nickname;
            }

            bool isFriend = false;
            if (!isMe && currentFriendsList != null && !string.IsNullOrEmpty(officialUsername))
            {
                isFriend = currentFriendsList.Contains(officialUsername);
            }

            currentSlot.Username = officialUsername;
            currentSlot.Nickname = player.Nickname;
            currentSlot.IsReady = player.IsReady;
            currentSlot.IsLocalPlayer = isMe;
            currentSlot.CanKick = this.isHost && !isMe;
            currentSlot.IsGuest = player.IdPlayer <= 0;
            currentSlot.IsFriend = isFriend;
            currentSlot.ProfilePicture = player.ProfilePicture;
            currentSlot.LocalUserIsGuest = UserSession.Instance.GetPlayerId() <= 0;
            currentSlot.IdPlayer = player.IdPlayer;
        }

        public void StartTheGame(string matchCode, string username)
        {
            lobbyServiceClient.StartGame(matchCode);
        }

        public async Task CleanupBeforeClosingAsync()
        {
            CancelReconnectionAndExit();

            try
            {
                if (lobbyServiceClient is LobbyServiceClient serviceClient)
                {
                    serviceClient.ForceAbort();
                    serviceClient.StopConnectionMonitoring();
                    await serviceClient.LeaveLobbyAsync();
                }
                else
                {
                    lobbyServiceClient.LeaveLobby(UserSession.Instance.GetNickname());
                }

                if (Chat != null && Chat.IsConnected)
                {
                    await Chat.DisconnectAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOBBY VM] CleanupBeforeClosingAsync error: {ex}");
            }
            finally
            {
                CleanupLocalStateOnly();
            }
        }

        private void CleanupLocalStateOnly()
        {
            try
            {
                UserSession.Instance.CurrentMatchCode = string.Empty;

                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    foreach (var slot in Slots)
                    {
                        slot.Username = "";
                        slot.Nickname = "";
                        slot.IsReady = false;
                        slot.IsLocalPlayer = false;
                        slot.CanKick = false;
                        slot.IsFriend = false;
                        slot.ProfilePicture = null;
                    }
                });

                if (lobbyServiceClient != null)
                {
                    lobbyServiceClient.ConnectionError -= OnLobbyConnectionError;
                    lobbyServiceClient.PlayerKickedEvent -= OnPlayerKicked;
                    lobbyServiceClient.PlayerLeft -= OnPlayerLeft;
                    lobbyServiceClient.LobbyInvitationReceived -= OnLobbyInvitationReceived;
                    lobbyServiceClient.PlayerListUpdated -= OnPlayerListUpdated;
                    lobbyServiceClient.GameStartedEvent -= OnGameStarted;
                    lobbyServiceClient.ConnectionLost -= OnConnectionTimerExpired;
                }

                if (Chat != null)
                {
                    Chat.ChatDegraded -= OnChatDegraded;
                    Chat.RequestWindowClose -= OnChatRequestWindowClose;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOBBY VM] CleanupLocalStateOnly error: {ex}");
            }
        }


        public void LeaveOfTheLobby(string nickname)
        {
            try
            {
                Debug.WriteLine($"[LOBBY VM] Trying to exit from the lobby: {nickname}");

                if (Chat != null && Chat.IsConnected)
                {
                    try
                    {
                        var disconnectTask = Chat.DisconnectAsync();
                        Debug.WriteLine($"[LOBBY VM] Chat disconnected for {nickname}");
                    }
                    catch (Exception chatEx)
                    {
                        Debug.WriteLine($"[LOBBY VM] Error disconnecting from chat: {chatEx.Message}");
                    }
                }

                lobbyServiceClient.LeaveLobby(nickname);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOBBY VM] Error while exiting from lobby: {ex.Message}");
            }
            finally
            {
                UserSession.Instance.CurrentMatchCode = null;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var slot in Slots)
                    {
                        slot.Username = "";
                        slot.Nickname = "";
                        slot.IsReady = false;
                        slot.IsLocalPlayer = false;
                        slot.CanKick = false;
                    }
                });

            }
        }

        public void InvitePlayerByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(MatchCode)) return;
            string sender = UserSession.Instance.CurrentUser.Username;
            _ = lobbyServiceClient.SendLobbyInviteByEmail(email, MatchCode, sender);
            MessageBox.Show(Lang.Lobby_EmailSended);
        }

        public event Action NavigateToGame;

        private async void OnGameStarted(string matchCode)
        {
            try
            {
                Debug.WriteLine("[LOBBY VM] Game starting - stopping lobby connection monitoring");

                if (lobbyServiceClient is LobbyServiceClient serviceClient)
                {
                    serviceClient.StopConnectionMonitoring();
                    Debug.WriteLine("[LOBBY VM] ✅ Lobby monitoring stopped successfully");
                }

                if (Chat != null && Chat.IsConnected)
                {
                    await Chat.DisconnectAsync();
                    Debug.WriteLine("[LOBBY VM] ✅ Chat disconnected before navigating to match");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOBBY VM] ⚠️ Error during game start cleanup: {ex.Message}");
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                Debug.WriteLine("[LOBBY VM] 🎮 Navigating to game window");
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
                        IdPlayer = slot.IdPlayer,
                        IsHost = slot.IsLocalPlayer && this.isHost,
                        IsReady = slot.IsReady
                    });
                }
            }

            return players;
        }

        public bool CurrentClientIsHost() => isHost;

        public int GetPlayersCount() => Slots.Count(s => !string.IsNullOrEmpty(s.Username));

        public int GetMyLobbyUserId()
        {
            return this.MyActualPlayerId;
        }

        public void SetWaitingForGuestCallback(bool waiting) { }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public async Task ConnectChatAsync()
        {
            if (string.IsNullOrEmpty(MatchCode))
            {
                return;
            }

            try
            {
                await Chat.ConnectAsync(
                    UserSession.Instance.CurrentUser.Username,
                    UserSession.Instance.CurrentUser.IdUser,
                    context: 0,
                    matchCode: MatchCode
                );
            }
            catch (CommunicationException ex)
            {
                OnChatDegraded(Lang.Chat_CannotConnect);
            }
            catch (TimeoutException ex)
            {
                OnChatDegraded(Lang.Chat_CannotConnect);
            }
            catch (Exception ex)
            {
                OnChatDegraded(Lang.Chat_CannotConnect);
            }
        }

        public void Cleanup()
        {
            try
            {
                StopMonitoringAndReconnection();
                CleanupServices();
                UnsubscribeFromEvents();
                CleanupFriends();

                UserSession.Instance.CurrentMatchCode = string.Empty;
                Debug.WriteLine("[LOBBY VM] ✅ Sesión limpiada");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOBBY VM] ❌ Error general en Cleanup: {ex.Message}");
                Debug.WriteLine($"[LOBBY VM] StackTrace: {ex.StackTrace}");
            }
            finally
            {
                Debug.WriteLine("[LOBBY VM] Cleanup finalizado");
            }
        }

        private void StopMonitoringAndReconnection()
        {
            if (lobbyServiceClient is LobbyServiceClient serviceClient)
            {
                serviceClient.StopConnectionMonitoring();
            }

            if (isAttemptingReconnection)
            {
                Debug.WriteLine("[LOBBY VM] Deteniendo intentos de reconexión en Cleanup...");
                userRequestedExit = true;
                StopReconnectionAttempts(success: false);
            }
        }

        private void CleanupServices()
        {
            if (Chat != null)
            {
                try
                {
                    Debug.WriteLine("[LOBBY VM] Limpiando chat...");
                    Chat.ChatDegraded -= OnChatDegraded;
                    Chat.RequestWindowClose -= OnChatRequestWindowClose;
                    var disconnectTask = Chat.DisconnectAsync();
                    Debug.WriteLine("[LOBBY VM] ✅ Chat desconectado");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LOBBY VM] ⚠️ Error limpiando chat (ignorado): {ex.Message}");
                }
            }

            string myUsername = UserSession.Instance.CurrentUser?.Username;
            if (!string.IsNullOrEmpty(myUsername))
            {
                Debug.WriteLine($"[LOBBY VM] Intentando desconectar del lobby: {myUsername}");


                _ = Task.Run(() =>
                {
                    try
                    {
                        lobbyServiceClient.LeaveLobby(myUsername);
                        Debug.WriteLine("[LOBBY VM] ✅ Desconectado del lobby");
                    }
                    catch (CommunicationException)
                    {
                        Debug.WriteLine("[LOBBY VM] ⏱️ Sin internet - servidor limpiará la sesión");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[LOBBY VM] ⚠️ Error desconectando: {ex.Message}");
                    }
                });

                Debug.WriteLine("[LOBBY VM] Desconexión iniciada en background");
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (lobbyServiceClient != null)
            {
                Debug.WriteLine("[LOBBY VM] Desuscribing events from lobby...");
                lobbyServiceClient.ConnectionError -= OnLobbyConnectionError;
                lobbyServiceClient.PlayerKickedEvent -= OnPlayerKicked;
                lobbyServiceClient.PlayerLeft -= OnPlayerLeft;
                lobbyServiceClient.LobbyInvitationReceived -= OnLobbyInvitationReceived;
                lobbyServiceClient.PlayerListUpdated -= OnPlayerListUpdated;
                lobbyServiceClient.GameStartedEvent -= OnGameStarted;
                lobbyServiceClient.ConnectionLost -= OnConnectionTimerExpired;
                Debug.WriteLine("[LOBBY VM] ✅ Eventos de lobby desuscritos");
            }
        }

        private void CleanupFriends()
        {
            if (Friends != null)
            {
                try
                {
                    Debug.WriteLine("[LOBBY VM] Limpiando sistema de amigos...");
                    string myUser = UserSession.Instance.CurrentUser?.Username;
                    if (!string.IsNullOrEmpty(myUser))
                    {

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var unsubTask = Friends.UnsubscribeAsync(myUser);
                                var completedTask = await Task.WhenAny(unsubTask, Task.Delay(2000));

                                if (completedTask == unsubTask)
                                {
                                    Debug.WriteLine("[LOBBY VM] ✅ Amigos desuscritos correctamente");
                                }
                                else
                                {
                                    Debug.WriteLine("[LOBBY VM] ⏱️ Timeout al desuscribir amigos (sin internet)");
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[LOBBY VM] ⚠️ Error async desuscribiendo Friends: {ex.Message}");
                            }
                        });

                        Task.Delay(150).Wait();
                    }

                    Friends.Dispose();
                    Debug.WriteLine("[LOBBY VM] ✅ Sistema de amigos limpiado");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LOBBY VM] ⚠️ Error limpiando Friends (ignorado): {ex.Message}");
                }
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
                Debug.WriteLine($"[LOBBY VM] Player leave: {playerDto.Nickname}");

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

        private void OnLobbyInvitationReceived(LobbyInvitationDTO invitation)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"[LOBBY VM] Lobby invitation received from {invitation.SenderNickname} for lobby {invitation.LobbyCode}");
                ShowInvitationDialog(invitation);
            });
        }

        private async void ShowInvitationDialog(LobbyInvitationDTO invitation)
        {
            var result = MessageBox.Show(
                $"{invitation.SenderNickname}" + Lang.Lobby_InvitationReceived, Lang.Lobby_InvitationReceivedTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                await JoinInvitedLobby(invitation);
            }
        }

        private async Task JoinInvitedLobby(LobbyInvitationDTO invitation)
        {
            try
            {
                await LeavePreviousLobbyIfNeeded(invitation.LobbyCode);

                var userAccount = CreateUserAccountDTO();
                var joinResult = await JoinNewLobby(userAccount, invitation.LobbyCode);

                if (joinResult == JoinMatchResultCode.JoinMatch_Success)
                {
                    await CompleteSuccessfulJoin(invitation.LobbyCode, userAccount.Nickname);
                }
                else
                {
                    HandleJoinFailure(joinResult);
                }
            }
            catch (Exception ex)
            {
                HandleJoinException(ex);
            }
        }

        private async Task LeavePreviousLobbyIfNeeded(string newLobbyCode)
        {
            string currentLobby = UserSession.Instance.CurrentMatchCode;

            if (string.IsNullOrEmpty(currentLobby) || currentLobby == newLobbyCode)
            {
                return;
            }

            string myNickname = UserSession.Instance.CurrentUser.Nickname;
            lobbyServiceClient.LeaveLobby(myNickname);

            UserSession.Instance.CurrentMatchCode = string.Empty;

            await Task.Delay(1000);
        }

        private ArchsVsDinosClient.DTO.UserAccountDTO CreateUserAccountDTO()
        {
            return new ArchsVsDinosClient.DTO.UserAccountDTO
            {
                Nickname = UserSession.Instance.CurrentUser.Nickname,
                Username = UserSession.Instance.CurrentUser.Username,
                IdPlayer = UserSession.Instance.CurrentUser.IdUser
            };
        }

        private async Task<JoinMatchResultCode> JoinNewLobby(ArchsVsDinosClient.DTO.UserAccountDTO userAccount, string lobbyCode)
        {
            return await lobbyServiceClient.JoinLobbyAsync(userAccount, lobbyCode);
        }

        private async Task CompleteSuccessfulJoin(string lobbyCode, string nickname)
        {
            UserSession.Instance.CurrentMatchCode = lobbyCode;

            Application.Current.Dispatcher.Invoke(() =>
            {
                this.MatchCode = lobbyCode;
                this.IsHost = false;
            });

            await lobbyServiceClient.ConnectToLobbyAsync(lobbyCode, nickname);

            if (lobbyServiceClient is LobbyServiceClient serviceClient)
            {
                serviceClient.StartConnectionMonitoring(timeoutSeconds: 12);
            }

            await Task.Delay(1500);
            await ReconnectChatToNewLobby(lobbyCode);
            await VerifyLobbyState();
        }

        private async Task VerifyLobbyState()
        {
            await Task.Delay(1000);

            Application.Current.Dispatcher.Invoke(() =>
            {
                int playersInSlots = Slots.Count(s => !string.IsNullOrEmpty(s.Nickname));

                if (playersInSlots == 0)
                {
                    MessageBox.Show(
                        Lang.Lobby_PlayerUploadError,
                        Lang.GlobalError,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            });
        }

        private async Task ReconnectChatToNewLobby(string lobbyCode)
        {
            if (Chat == null)
            {
                return;
            }

            try
            {
                await Chat.DisconnectAsync();
                await Task.Delay(500);
                await Chat.ConnectAsync(
                    UserSession.Instance.CurrentUser.Username,
                    UserSession.Instance.CurrentUser.IdUser,
                    context: 0,
                    matchCode: lobbyCode
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOBBY VM] Error reconectando chat: {ex.Message}");
            }
        }

        private void HandleJoinFailure(JoinMatchResultCode resultCode)
        {
            string errorMsg = LobbyResultCodeHelper.GetMessage(resultCode);
            MessageBox.Show(
                Lang.Lobby_ErrorOccured,
                Lang.GlobalError,
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        private void HandleJoinException(Exception ex)
        {
            Debug.WriteLine($"[LOBBY VM] Error en JoinInvitedLobby: {ex.Message}");
            MessageBox.Show(
                Lang.Lobby_ErrorOccured,
                Lang.GlobalError,
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        public async void InviteFriendToLobby(string friendUsername)
        {
            if (string.IsNullOrWhiteSpace(MatchCode))
            {
                MessageBox.Show(Lang.Lobby_NoActiveLobbyMessage, Lang.GlobalError, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(friendUsername))
            {
                MessageBox.Show(Lang.Lobby_SelectAFriendMessage, Lang.GlobalError, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string senderNickname = UserSession.Instance.CurrentUser.Nickname;
            bool sent = await lobbyServiceClient.SendLobbyInviteToFriendAsync(MatchCode, senderNickname, friendUsername);

            if (sent)
            {
                string message = string.Format(Lang.Lobby_InvitationSentMessage, friendUsername);
                MessageBox.Show(message, Lang.Lobby_InvitationSentTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                _ = CheckIfFriendJoined(friendUsername);
            }
            else
            {
                MessageBox.Show(Lang.Lobby_InvitationNotSent, Lang.GlobalError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CheckIfFriendJoined(string friendUsername)
        {
            await Task.Delay(30000);

            bool friendJoined = Slots.Any(s =>
                !string.IsNullOrEmpty(s.Username) &&
                s.Username.Equals(friendUsername, StringComparison.OrdinalIgnoreCase));

            if (!friendJoined)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    string message = string.Format(Lang.Lobby_TryEmailMessage, friendUsername);
                    var result = MessageBox.Show(
                        message,
                        Lang.Lobby_TryEmailTitle,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        MessageBox.Show(Lang.Lobby_TryClicking, Lang.GlobalInformation, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                });
            }
        }

        public void StartReconnectionAttempts()
        {
            if (string.IsNullOrEmpty(MatchCode))
            {
                Debug.WriteLine("[LOBBY VM] ⚠️ No hay matchCode para reconectar");
                return;
            }

            if (isAttemptingReconnection)
            {
                Debug.WriteLine("[LOBBY VM] ⚠️ Ya hay un intento de reconexión en curso");
                return;
            }

            Debug.WriteLine("[LOBBY VM] 🔄 Iniciando intentos de reconexión automática...");
            isAttemptingReconnection = true;
            reconnectionAttempts = 0;
            userRequestedExit = false;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var result = MessageBox.Show(
                    Lang.Lobby_TryReconnectMessage,
                    Lang.Lobby_TryReconnectionTitle,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.No)
                {
                    Debug.WriteLine("[LOBBY VM] Usuario eligió regresar, cancelando reconexión...");
                    userRequestedExit = true;
                    StopReconnectionAttempts(success: false);

                    LobbyConnectionLost?.Invoke(
                        Lang.Lobby_LeavingLobby,
                        Lang.Lobby_ComingBackToMainWindow
                    );
                    return;
                }
            });

            if (!userRequestedExit)
            {
                reconnectionTimer = new System.Timers.Timer(RECONNECTION_INTERVAL_MS);
                reconnectionTimer.Elapsed += OnReconnectionTimerElapsed;
                reconnectionTimer.AutoReset = true;
                reconnectionTimer.Start();
            }
        }

        private async void OnReconnectionTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {

            if (lobbyServiceClient is LobbyServiceClient serviceClient)
            {
                serviceClient.ForceAbort();
            }


            if (userRequestedExit)
            {
                Debug.WriteLine("[LOBBY VM] Reconexión cancelada por el usuario");
                StopReconnectionAttempts(success: false);
                return;
            }

            reconnectionAttempts++;

            Debug.WriteLine($"[LOBBY VM] Intento de reconexión #{reconnectionAttempts}/{MAX_RECONNECTION_ATTEMPTS}");

            if (reconnectionAttempts > MAX_RECONNECTION_ATTEMPTS)
            {
                StopReconnectionAttempts(success: false);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        Lang.Lobby_NoReconnectionMessage,
                        Lang.Lobby_NoReconnectionTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );

                    LobbyConnectionLost?.Invoke(
                        Lang.Lobby_NoReconnectionTitle,
                        Lang.Lobby_ReconnectionFailed
                    );
                });

                return;
            }

            string myNickname = UserSession.Instance.GetNickname();
            bool reconnected = await lobbyServiceClient.TryReconnectToLobbyAsync(MatchCode, myNickname);

            if (reconnected)
            {
                Debug.WriteLine($"[LOBBY VM] ✅ Reconexión exitosa después de {reconnectionAttempts} intentos");
                StopReconnectionAttempts(success: true);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        Lang.Lobby_ReconnectionReestablishedMessage,
                        Lang.Lobby_ConnectionReestablishedTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                });

                await Task.Delay(1000);

            }
        }

        private void StopReconnectionAttempts(bool success)
        {
            if (reconnectionTimer != null)
            {
                reconnectionTimer.Stop();
                reconnectionTimer.Elapsed -= OnReconnectionTimerElapsed;
                reconnectionTimer.Dispose();
                reconnectionTimer = null;
            }

            isAttemptingReconnection = false;
            reconnectionAttempts = 0;
            userRequestedExit = false;

            if (success && lobbyServiceClient is LobbyServiceClient serviceClient)
            {
                serviceClient.StartConnectionMonitoring(timeoutSeconds: 12);
                Debug.WriteLine("[LOBBY VM] Connection monitoring restarted after successful reconnection");
            }

            Debug.WriteLine($"[LOBBY VM] Intentos de reconexión detenidos. Éxito: {success}");
        }

        public void CancelReconnectionAndExit()
        {
            Debug.WriteLine("[LOBBY VM] CancelReconnectionAndExit llamado");
            userRequestedExit = true;

            if (isAttemptingReconnection)
            {
                StopReconnectionAttempts(success: false);
            }
        }

    }
}