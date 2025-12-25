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
        private const int MAX_RECONNECTION_ATTEMPTS = 5;
        private const int RECONNECTION_INTERVAL_MS = 5000;

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
            }

            string myUsername = UserSession.Instance.CurrentUser.Username;
            this.Friends = new FriendRequestViewModel(myUsername);
            this.Friends.Subscribe(myUsername);

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

                if (isInitializing)
                {
                    Debug.WriteLine($"[LOBBY VM] Error ignorado durante inicialización");
                    return;
                }

                if (!string.IsNullOrEmpty(MatchCode) && !isAttemptingReconnection)
                {
                    Debug.WriteLine($"[LOBBY VM] 🔄 Lobby activo detectado, iniciando reconexión automática...");
                    StartReconnectionAttempts();
                }
                else if (LobbyConnectionLost != null && !isAttemptingReconnection)
                {
                    LobbyConnectionLost(
                        "Conexión perdida",
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
                IdPlayer = UserSession.Instance.CurrentPlayer?.IdPlayer ?? 0
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
                    "Error de conexión",
                    "No se pudo conectar con el servidor."
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
                    this.MyActualPlayerId = meInList.IdPlayer;
                    this.myCurrentLobbyId = meInList.IdPlayer;

                    if (UserSession.Instance.GetPlayerId() == 0 && meInList.IdPlayer != 0)
                    {
                        if (UserSession.Instance.CurrentUser != null)
                            UserSession.Instance.CurrentUser.IdUser = meInList.IdPlayer;

                        if (UserSession.Instance.CurrentPlayer != null)
                            UserSession.Instance.CurrentPlayer.IdPlayer = meInList.IdPlayer;

                        Debug.WriteLine($"[LOBBY] ID Local Actualizado: {meInList.IdPlayer}");
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
            try
            {
                Debug.WriteLine($"[LOBBY VM] Intentando salir del lobby: {nickname}");
                lobbyServiceClient.LeaveLobby(nickname);
                Debug.WriteLine($"[LOBBY VM] Comando de salida enviado");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOBBY VM] ⚠️ Error al salir del lobby (ignorado): {ex.Message}");
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

                Debug.WriteLine($"[LOBBY VM] Estado local limpiado");
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
            if (string.IsNullOrEmpty(MatchCode))
            {
                return;
            }

            try
            {
                await Chat.ConnectAsync(
                    UserSession.Instance.CurrentUser.Username,
                    context: 0,
                    matchCode: MatchCode
                );
            }
            catch (CommunicationException ex)
            {
                OnChatDegraded($"No se pudo conectar al chat: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                OnChatDegraded($"Timeout al conectar al chat: {ex.Message}");
            }
            catch (Exception ex)
            {
                OnChatDegraded($"Error al conectar al chat: {ex.Message}");
            }
        }

        public void Cleanup()
        {
            Debug.WriteLine("[LOBBY VM] ═══════════════════════════════════════");
            Debug.WriteLine("[LOBBY VM] Iniciando Cleanup...");

            try
            {
                // Detener reconexión automática si está activa
                if (isAttemptingReconnection)
                {
                    Debug.WriteLine("[LOBBY VM] Deteniendo intentos de reconexión...");
                    StopReconnectionAttempts(success: false);
                }

                // Intentar desconectar del lobby
                string myUsername = UserSession.Instance.CurrentUser?.Username;
                if (!string.IsNullOrEmpty(myUsername))
                {
                    try
                    {
                        Debug.WriteLine($"[LOBBY VM] Intentando desconectar: {myUsername}");
                        LeaveOfTheLobby(myUsername);
                        Debug.WriteLine($"[LOBBY VM] ✅ Desconexión completada");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[LOBBY VM] ⚠️ Error en desconexión (ignorado): {ex.Message}");
                    }
                }

                if (lobbyServiceClient != null)
                {
                    Debug.WriteLine("[LOBBY VM] Desuscribiendo eventos de lobby...");
                    lobbyServiceClient.ConnectionError -= OnLobbyConnectionError;
                    lobbyServiceClient.PlayerKickedEvent -= OnPlayerKicked;
                    lobbyServiceClient.PlayerLeft -= OnPlayerLeft;
                    lobbyServiceClient.LobbyInvitationReceived -= OnLobbyInvitationReceived;
                    lobbyServiceClient.PlayerListUpdated -= OnPlayerListUpdated;
                    lobbyServiceClient.GameStartedEvent -= OnGameStarted;
                    Debug.WriteLine("[LOBBY VM] ✅ Eventos de lobby desuscritos");
                }

                // Limpiar chat
                if (Chat != null)
                {
                    try
                    {
                        Debug.WriteLine("[LOBBY VM] Limpiando chat...");
                        Chat.ChatDegraded -= OnChatDegraded;
                        Chat.RequestWindowClose -= OnChatRequestWindowClose;

                        // Fire-and-forget
                        var disconnectTask = Chat.DisconnectAsync();
                        Debug.WriteLine("[LOBBY VM] ✅ Chat desconectado");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[LOBBY VM] ⚠️ Error limpiando chat (ignorado): {ex.Message}");
                    }
                }

                // Limpiar amigos
                if (Friends != null)
                {
                    try
                    {
                        Debug.WriteLine("[LOBBY VM] Limpiando sistema de amigos...");
                        string myUser = UserSession.Instance.CurrentUser?.Username;
                        if (!string.IsNullOrEmpty(myUser))
                        {
                            Friends.Unsubscribe(myUser);
                        }
                        Friends.Dispose();
                        Debug.WriteLine("[LOBBY VM] ✅ Sistema de amigos limpiado");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[LOBBY VM] ⚠️ Error limpiando Friends (ignorado): {ex.Message}");
                    }
                }

                UserSession.Instance.CurrentMatchCode = null;
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
                Debug.WriteLine("[LOBBY VM] ═══════════════════════════════════════");
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

        private void OnLobbyInvitationReceived(LobbyInvitationDTO invitation)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"[LOBBY VM] Invitación recibida de {invitation.SenderNickname} para lobby {invitation.LobbyCode}");
                ShowInvitationDialog(invitation);
            });
        }

        private async void ShowInvitationDialog(LobbyInvitationDTO invitation)
        {
            var result = MessageBox.Show(
                $"{invitation.SenderNickname} te ha invitado a unirte a su lobby.\n\n¿Deseas aceptar la invitación?",
                "Invitación de lobby",
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

            UserSession.Instance.CurrentMatchCode = null;

            await Task.Delay(1000);
        }

        private ArchsVsDinosClient.DTO.UserAccountDTO CreateUserAccountDTO()
        {
            return new ArchsVsDinosClient.DTO.UserAccountDTO
            {
                Nickname = UserSession.Instance.CurrentUser.Nickname,
                Username = UserSession.Instance.CurrentUser.Username,
                IdPlayer = UserSession.Instance.CurrentPlayer?.IdPlayer ?? 0
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
                        "Error de sincronización: No se cargaron los jugadores del lobby.\n\nPor favor, sal y vuelve a unirte al lobby.",
                        "Error",
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
                $"No se pudo unir al lobby: {errorMsg}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        private void HandleJoinException(Exception ex)
        {
            Debug.WriteLine($"[LOBBY VM] Error en JoinInvitedLobby: {ex.Message}");
            MessageBox.Show(
                "Ocurrió un error al intentar unirse al lobby.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        public async void InviteFriendToLobby(string friendUsername)
        {
            if (string.IsNullOrWhiteSpace(MatchCode))
            {
                MessageBox.Show("No hay un lobby activo.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(friendUsername))
            {
                MessageBox.Show("Selecciona un amigo para invitar.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string senderNickname = UserSession.Instance.CurrentUser.Nickname;
            bool sent = await lobbyServiceClient.SendLobbyInviteToFriendAsync(MatchCode, senderNickname, friendUsername);

            if (sent)
            {
                MessageBox.Show($"Invitación enviada a {friendUsername}.", "Invitación enviada", MessageBoxButton.OK, MessageBoxImage.Information);
                _ = CheckIfFriendJoined(friendUsername);
            }
            else
            {
                MessageBox.Show("No se pudo enviar la invitación.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    var result = MessageBox.Show(
                        $"{friendUsername} no se ha unido al lobby.\n¿Deseas enviarle una invitación por correo?",
                        "Sin respuesta",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        MessageBox.Show("Funcionalidad de envío de correo pendiente.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
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

            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    "Se perdió la conexión con el servidor.\n\n" +
                    "Se intentará reconectar automáticamente durante los próximos 25 segundos.\n\n" +
                    "Puedes quedarte aquí o regresar al menú principal.",
                    "Reconexión automática",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            });

            reconnectionTimer = new System.Timers.Timer(RECONNECTION_INTERVAL_MS);
            reconnectionTimer.Elapsed += OnReconnectionTimerElapsed;
            reconnectionTimer.AutoReset = true;
            reconnectionTimer.Start();
        }

        private async void OnReconnectionTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            reconnectionAttempts++;

            Debug.WriteLine($"[LOBBY VM] 🔄 Intento de reconexión #{reconnectionAttempts}/{MAX_RECONNECTION_ATTEMPTS}");

            if (reconnectionAttempts > MAX_RECONNECTION_ATTEMPTS)
            {
                StopReconnectionAttempts(success: false);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        "No se pudo restablecer la conexión después de varios intentos.\n\n" +
                        "Serás redirigido al menú principal.",
                        "Reconexión fallida",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );

                    LobbyConnectionLost?.Invoke(
                        "Reconexión fallida",
                        "No se pudo restablecer la conexión con el servidor."
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
                        "¡Conexión restablecida exitosamente!\n\n" +
                        "Ya puedes continuar en el lobby.",
                        "Reconexión exitosa",
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

            Debug.WriteLine($"[LOBBY VM] Intentos de reconexión detenidos. Éxito: {success}");
        }
    }
}