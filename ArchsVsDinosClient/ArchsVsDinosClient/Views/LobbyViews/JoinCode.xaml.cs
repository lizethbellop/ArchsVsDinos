using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.LobbyService;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using ArchsVsDinosClient.ViewModels;
using System;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using UserAccountDTO = ArchsVsDinosClient.DTO.UserAccountDTO;

namespace ArchsVsDinosClient.Views.LobbyViews
{
    public partial class JoinCode : Window
    {
        public bool IsCancelled { get; private set; } = true;
        public string EnteredCode => TxtB_MatchCode.Text.Trim();
        private readonly ILobbyServiceClient lobbyServiceClient;

        public JoinCode()
        {
            InitializeComponent();
            lobbyServiceClient = new LobbyServiceClient();

            /*
            if (UserSession.Instance.CurrentUser == null)
            {
                UserSession.Instance.LoginAsGuest();
            }*/
        }

        private async void Click_BtnAccept(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            string code = EnteredCode;

            if (string.IsNullOrEmpty(code))
            {
                MessageBox.Show(Lang.JoinCode_Invalid);
                return;
            }

            try
            {
                var userAccount = BuildUserAccount();
                UserSession.Instance.CurrentMatchCode = code;

                var result = await lobbyServiceClient.JoinLobbyAsync(userAccount, code);

                if (result == JoinMatchResultCode.JoinMatch_Success)
                {
                    string nickname = UserSession.Instance.GetNickname();

                    var lobbyWindow = new Lobby(false, lobbyServiceClient);
                    var lobbyViewModel = (LobbyViewModel)lobbyWindow.DataContext;

                    if (userAccount.IdPlayer == 0)
                    {
                        lobbyViewModel.SetWaitingForGuestCallback(true);
                    }

                    Debug.WriteLine($"[JOINCODE] Connecting to lobby {code} as {nickname}...");
                    await lobbyServiceClient.ConnectToLobbyAsync(code, nickname);
                    Debug.WriteLine($"[JOINCODE] Connected successfully");

                    lobbyWindow.Show();
                    IsCancelled = false;
                    Application.Current.MainWindow = lobbyWindow;

                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is MainWindow main)
                        {
                            main.IsNavigating = true;
                            main.Close();
                            break;
                        }
                    }

                    Application.Current.MainWindow = lobbyWindow;
                    IsCancelled = false;
                    this.Close();
                }
                else
                {
                    string msg = LobbyResultCodeHelper.GetMessage(result);
                    Debug.WriteLine($"[JOINCODE] Join failed: {msg}");
                    MessageBox.Show(msg);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JOINCODE] Error joining lobby: {ex.Message}");
                MessageBox.Show(Lang.JoinMatch_ErrorJoinMatch);
            }
        }

        private UserAccountDTO BuildUserAccount()
        {
            var user = UserSession.Instance.CurrentUser;

            if (user == null)
            {
                string guestNickname = UserSession.Instance.GetNickname();
                if (string.IsNullOrEmpty(guestNickname))
                {
                    guestNickname = "Guest_" + new Random().Next(1000, 9999);
                    UserSession.Instance.SetGuestSession(guestNickname, guestNickname);
                }

                Debug.WriteLine($"[JOINCODE] Joining as guest: {guestNickname}");

                return new UserAccountDTO
                {
                    Nickname = guestNickname,
                    Username = guestNickname, 
                    IdPlayer = 0 
                };
            }

            Debug.WriteLine($"[JOINCODE] Joining as registered user: {user.Nickname} | UserID: {user.IdUser}");

            return new UserAccountDTO
            {
                Nickname = user.Nickname,
                Username = user.Username,
                IdPlayer = user.IdUser    
            };
        }

        private void Click_BtnCancel(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            IsCancelled = true;
            this.Close();
        }
    }
}