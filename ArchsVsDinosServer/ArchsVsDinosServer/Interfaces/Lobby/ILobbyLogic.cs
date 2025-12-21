using Contracts.DTO;
using Contracts.DTO.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Interfaces.Lobby
{
    public interface ILobbyLogic
    {
        Task<MatchCreationResponse> CreateLobby(MatchSettings settings);
        Task<MatchJoinResponse> JoinLobby(string lobbyCode, int userId, string nickname);
        Task<bool> SendInvitations(string lobbyCode, string sender, List<string> guests);
        void ConnectPlayer(string lobbyCode, string playerNickname);
        void DisconnectPlayer(string lobbyCode, string playerNickname);

        Task UpdatePlayerReadyStatus(string lobbyCode, string playerName, bool isReady);

        Task EvaluateGameStart(string lobbyCode, int userID);

        void KickPlayer(string lobbyCode, int hostUserId, string targetNickname);
    }
}
