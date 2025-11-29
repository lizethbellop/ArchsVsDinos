using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.LobbyService;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.RegisterService;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using ArchsVsDinosClient.ViewModels;
using System;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using LobbyPlayerDTO = ArchsVsDinosClient.LobbyService.LobbyPlayerDTO;
using UserAccountDTO = ArchsVsDinosClient.LobbyService.UserAccountDTO;

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
                var lobbyWindow = new Lobby(false, lobbyServiceClient);
                var lobbyViewModel = (LobbyViewModel)lobbyWindow.DataContext;

                UserSession.Instance.CurrentMatchCode = code;

                var result = await Task.Run(() => lobbyServiceClient.JoinLobby(userAccount, code));

                if (result == LobbyResultCode.Lobby_LobbyJoined)
                {
                    lobbyWindow.Show();
                    IsCancelled = false;
                    Application.Current.MainWindow = lobbyWindow;

                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is MainWindow)
                        {
                            window.Close();
                            break;
                        }
                    }
                    this.Close();
                }
                else
                {
                    lobbyWindow.Close();
                    MessageBox.Show(Lang.JoinCode_InvalidOrFull);
                }
            }
            catch (TimeoutException)
            {
                MessageBox.Show(Lang.JoinMatch_ErrorJoinMatch);
            }
            catch (CommunicationException)
            {
                MessageBox.Show(Lang.JoinMatch_ErrorJoinMatch);
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

            return new UserAccountDTO
            {
                Name = user.Name,
                Email = user.Email,
                Username = user.Username,
                Nickname = user.Nickname,

                Password = string.Empty,

                IdConfiguration = 0,

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
