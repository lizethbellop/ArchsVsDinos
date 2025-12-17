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

            if (UserSession.Instance.CurrentUser == null)
            {
                UserSession.Instance.LoginAsGuest();
            }
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
                var lobbyWindow = new Lobby(false, lobbyServiceClient);
                var lobbyViewModel = (LobbyViewModel)lobbyWindow.DataContext;

                if (userAccount.IdPlayer == 0)
                {
                    lobbyViewModel.SetWaitingForGuestCallback(true);
                }

                var result = await Task.Run(() => lobbyServiceClient.JoinLobbyAsync(userAccount, code));

                if (result == JoinMatchResultCode.JoinMatch_Success)
                {
                    lobbyServiceClient.ConnectToLobby(code, userAccount.Nickname);

                    lobbyWindow.Show();
                    IsCancelled = false;
                    Application.Current.MainWindow = lobbyWindow;

                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is MainWindow) { window.Close(); break; }
                    }
                    this.Close();
                }
                else
                {
                    lobbyWindow.Close();
                    string msg = LobbyResultCodeHelper.GetMessage(result);
                    Debug.WriteLine(msg);
                    MessageBox.Show(msg);
                }
            }
            catch (Exception)
            {
                MessageBox.Show(Lang.JoinMatch_ErrorJoinMatch);
            }
        }

        private UserAccountDTO BuildUserAccount()
        {
            var user = UserSession.Instance.CurrentUser;
            var player = UserSession.Instance.CurrentPlayer;

            if (user == null)
            {
                string guestNickname = "Guest_" + new Random().Next(1000, 9999);

                UserSession.Instance.SetGuestSession(guestNickname, guestNickname);

                return new UserAccountDTO
                {
                    Nickname = guestNickname,
                    IdPlayer = 0
                };
            }

            return new UserAccountDTO
            {
                Nickname = user.Nickname,
                IdPlayer = player?.IdPlayer ?? 0
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