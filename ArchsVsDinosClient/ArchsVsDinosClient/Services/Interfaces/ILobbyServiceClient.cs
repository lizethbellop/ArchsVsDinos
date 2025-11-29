using ArchsVsDinosClient.LobbyService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services.Interfaces
{
    public interface ILobbyServiceClient
    {
        event Action<LobbyPlayerDTO, string> LobbyCreated;
        event Action<LobbyPlayerDTO> PlayerJoined;
        event Action<LobbyPlayerDTO> PlayerLeft;
        event Action<LobbyPlayerDTO> PlayerExpelled;
        event Action<string> LobbyCancelled;
        event Action<string, List<LobbyPlayerDTO>> GameStartedEvent;

        void CreateLobby(UserAccountDTO userAccount);
        LobbyResultCode JoinLobby(UserAccountDTO userAccount, string matchCode);
        void LeaveLobby(string username);
        void ExpelPlayer(string targetUsername, string hostUsername);
        void CancellLobby(string matchCode, string usernameRequester);
        void StartGame(string matchCode, string hostUsername);
        LobbyResultCode SendLobbyInviteByEmail(string email, string matchCode, string senderUsername);

    }
}
