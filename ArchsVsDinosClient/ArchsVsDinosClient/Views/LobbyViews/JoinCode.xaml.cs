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

        /*
        private UserAccountDTO BuildUserAccount()
        {
            var user = UserSession.Instance.CurrentUser;
            var player = UserSession.Instance.CurrentPlayer;

            if (user == null)
            {
                string guestNickname = "Guest_" + new Random().Next(1000, 9999);

                UserSession.Instance.SetGuestSession(guestNickname, guestNickname);

                Debug.WriteLine($"[JOINCODE] Guest session created: {guestNickname}");

                return new UserAccountDTO
                {
                    Nickname = guestNickname,
                    IdPlayer = 0
                };
            }

            return new UserAccountDTO
            {
                Nickname = user.Nickname,
                Username = user.Username,
                IdPlayer = user.IdUser
            };
        }*/

        private UserAccountDTO BuildUserAccount()
        {
            var user = UserSession.Instance.CurrentUser;

            // CASO 1: Es un invitado (no inició sesión)
            if (user == null)
            {
                // Solo generamos un nombre nuevo si no tiene uno ya (para no cambiarle el nombre si reintenta)
                string guestNickname = UserSession.Instance.GetNickname();
                if (string.IsNullOrEmpty(guestNickname))
                {
                    guestNickname = "Guest_" + new Random().Next(1000, 9999);
                    UserSession.Instance.SetGuestSession(guestNickname, guestNickname);
                }

                Debug.WriteLine($"[JOINCODE] Uniéndose como invitado: {guestNickname}");

                return new UserAccountDTO
                {
                    Nickname = guestNickname,
                    Username = guestNickname, // El username es el mismo nickname en invitados
                    IdPlayer = 0 // El servidor le asignará el ID negativo
                };
            }

            // CASO 2: Es un usuario registrado (Abraham o Abi)
            Debug.WriteLine($"[JOINCODE] Uniéndose registrado: {user.Nickname} | UserID: {user.IdUser}");

            return new UserAccountDTO
            {
                Nickname = user.Nickname,
                Username = user.Username, // ¡IMPORTANTE! Se necesita para el JOIN de las fotos
                IdPlayer = user.IdUser    // MANDAMOS EL ID DE LA CUENTA (IdUser)
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