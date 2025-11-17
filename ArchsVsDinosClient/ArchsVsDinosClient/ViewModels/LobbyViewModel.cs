using ArchsVsDinosClient.LobbyService;
using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient.ViewModels
{
    public class LobbyViewModel
    {
        private readonly ILobbyServiceClient lobbyService;

        public ObservableCollection<LobbyPlayerDTO> Players { get; set; } = new ObservableCollection<LobbyPlayerDTO>();

        public event Action<LobbyPlayerDTO, string> LobbyCreated;

        public LobbyViewModel()
        {
            lobbyService = new LobbyServiceClient();
            lobbyService.LobbyCreated += LobbyService_LobbyCreated;

            var local = new LobbyPlayerDTO
            {
                Username = UserSession.Instance.CurrentUser.Username,
                Nickname = UserSession.Instance.CurrentUser.Nickname
            };
            Players.Add(local);
        }

        public void CreateLobby(UserAccountDTO userAccount)
        {
            try
            {
                lobbyService.CreateLobby(userAccount);
                MessageBox.Show(Lang.Lobby_LobbyCreated);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Lang.Lobby_ErrorCreatingLobby);
            }
        }

        private void LobbyService_LobbyCreated(LobbyPlayerDTO player, string lobbyId)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (player.Username != UserSession.Instance.CurrentUser.Username)
                {
                    Players.Add(player);
                    LobbyCreated?.Invoke(player, lobbyId);
                }
            });
        }
    }
}
