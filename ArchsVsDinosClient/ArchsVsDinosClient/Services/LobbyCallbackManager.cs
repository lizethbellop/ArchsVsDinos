using ArchsVsDinosClient.LobbyService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ArchsVsDinosClient.Services
{
    public class LobbyCallbackManager : ILobbyManagerCallback
    {
        public event Action<LobbyPlayerDTO, string> OnCreatedLobby;
        public event Action<LobbyPlayerDTO> OnJoinedLobby;
        public event Action<LobbyPlayerDTO> OnPlayerLeftLobby;
        public event Action<LobbyPlayerDTO> OnPlayerExpelled;
        public event Action<string> OnLobbyCancelled;

        public void CreatedLobby(LobbyPlayerDTO hostLobbyPlayerDTO, string matchCode)
        {
            OnCreatedLobby?.Invoke(hostLobbyPlayerDTO, matchCode);
        }

        public void JoinedLobby(LobbyPlayerDTO userAccountDTO)
        {
            OnJoinedLobby?.Invoke(userAccountDTO);
        }

        public void LeftLobby(LobbyPlayerDTO playerWhoLeft)
        {
            OnPlayerLeftLobby?.Invoke(playerWhoLeft);
        }

        public void ExpelledFromLobby(LobbyPlayerDTO expelledPlayer)
        {
            OnPlayerExpelled?.Invoke(expelledPlayer);
        }

        public void LobbyCancelled(string matchCode)
        {
            OnLobbyCancelled?.Invoke(matchCode);
        }
    }
}
