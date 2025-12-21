using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.LobbyService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LobbyPlayerDTO = ArchsVsDinosClient.DTO.LobbyPlayerDTO;

namespace ArchsVsDinosClient.Services.Interfaces
{
    public interface ILobbyServiceClient
    {
        Task<MatchCreationResultCode> CreateLobbyAsync(UserAccountDTO userAccount);
        Task<JoinMatchResultCode> JoinLobbyAsync(UserAccountDTO userAccount, string matchCode);

        void ConnectToLobby(string matchCode, string nickname);
        Task ConnectToLobbyAsync(string matchCode, string nickname);
        void LeaveLobby(string username);
        //void ExpelPlayer(string targetUsername, string hostUsername);
        //void CancellLobby(string matchCode, string usernameRequester);
        void StartGame(string matchCode);

        void KickPlayer(string lobbyCode, int hostUserId, string targetNickname);
        Task<bool> SendLobbyInviteByEmail(string email, string matchCode, string senderUsername);

        event Action<LobbyPlayerDTO, string> LobbyCreated;
        event Action<LobbyPlayerDTO> PlayerJoined;
        event Action<LobbyPlayerDTO> PlayerLeft;
        event Action<string> GameStartedEvent;
        event Action<string, bool> PlayerReadyEvent;
        event Action<string, string> ConnectionError;
        event Action<List<LobbyPlayerDTO>> PlayerListUpdated;
        event Action<string, string> PlayerKickedEvent;

    }
}
