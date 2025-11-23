using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.LobbyService;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.RegisterService;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.Utils;
using System;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
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
                var tempClient = new LobbyServiceClient();
                var result = await Task.Run(() => tempClient.JoinLobby(userAccount, code));


                if (result == LobbyResultCode.Lobby_LobbyJoined)
                {
                    UserSession.Instance.CurrentMatchCode = code; var lobby = new Lobby(false); lobby.Show(); IsCancelled = false; this.Close();
                }
                else
                {
                    MessageBox.Show(Lang.JoinCode_Invalid);
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
