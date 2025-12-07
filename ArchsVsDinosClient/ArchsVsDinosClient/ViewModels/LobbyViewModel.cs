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
        private bool waitingForGuestCallback = false;
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
                Nickname = UserSession.Instance.CurrentUser.Nickname,
                IdPlayer = UserSession.Instance.CurrentPlayer?.IdPlayer ?? 0
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

        public void InvitePlayerByEmail(string email)
        {
            if (UserSession.Instance.CurrentUser == null)
            {
                MessageBox.Show(Lang.Lobby_OnlyRegisteredInviteEmail);
                return;
            }

            var senderUsername = UserSession.Instance.CurrentUser.Username; 

            if (ValidationHelper.IsEmpty(email) || ValidationHelper.IsWhiteSpace(email))
            {
                MessageBox.Show(Lang.GlobalEmptyField);
            }
            else if (!ValidationHelper.IsAValidEmail(email))
            {
                MessageBox.Show(Lang.Register_InvalidEmail);
            }
            else
            {
                try
                {
                    var resultCode = lobbyServiceClient.SendLobbyInviteByEmail(email, MatchCode, senderUsername);

                    switch (resultCode)
                    {
                        case LobbyResultCode.Lobby_EmailSended:
                            MessageBox.Show(Lang.Lobby_EmailSended);
                            break;
                        case LobbyResultCode.Lobby_EmailSendError:
                            MessageBox.Show(Lang.Lobby_ErrorSendingEmail);
                            break;
                        default:
                            MessageBox.Show(Lang.GlobalServerError);
                            break;
                    }
                }
                catch
                {
                    MessageBox.Show(Lang.GlobalServerError);
                }
            }

        }

        public bool CurrentClientIsHost()
        {
            if (UserSession.Instance.CurrentUser == null)
            {
                return false;
            }
            return Players.FirstOrDefault(player => player.IsHost)?.Username == UserSession.Instance.CurrentUser.Username;
        } 

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
                    if (waitingForGuestCallback && !joiningPlayer.IsHost)
                    {
                        UserSession.Instance.SetGuestSession(joiningPlayer.Username, joiningPlayer.Nickname);
                        waitingForGuestCallback = false;
                    }
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
                }

                var mainWindow = new MainWindow();
                mainWindow.Show();

                var currentWindow = Application.Current.Windows
                    .OfType<Window>()
                    .FirstOrDefault(window => window is Views.LobbyViews.Lobby);

                currentWindow?.Close();
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

                if (UserSession.Instance.CurrentUser != null &&
                    expelledPlayer.Username == UserSession.Instance.CurrentUser.Username)
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
                var myUsername = UserSession.Instance.CurrentUser?.Username;
                var convertedPlayers = players.Select(player => new ArchsVsDinosClient.DTO.LobbyPlayerDTO
                {
                    Username = player.Username,
                    Nickname = player.Nickname,
                    IsHost = player.IsHost,
                    IdPlayer = player.IdPlayer,
                    ProfilePicture = player.ProfilePicture
                }).ToList();

                var match = new MainMatch(convertedPlayers, myUsername, matchCode);
                match.Show();

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is Lobby)
                        {
                            window.Close();
                            break;
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            });
        }

        public void SetWaitingForGuestCallback(bool waiting)
        {
            waitingForGuestCallback = waiting;
        }

        private void UpdateSlots()
        {
            var localUsername = UserSession.Instance.CurrentUser?.Username ?? "Guest";

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
                    Slots[i].ProfilePicture = player.ProfilePicture;
                    Slots[i].CanKick = CurrentClientIsHost() && player.Username != localUsername;
                    Slots[i].IsLocalPlayer = player.Username == localUsername;
                    Slots[i].IsFriend = false;
                }
                else
                {
                    Slots[i].Username = string.Empty;
                    Slots[i].Nickname = string.Empty;
                    Slots[i].IsFriend = false;
                    Slots[i].ProfilePicture = null;
                    Slots[i].CanKick = false;
                    Slots[i].IsLocalPlayer = false;

                }
            }
        }
    }
}
