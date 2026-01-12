using Contracts.DTO;
using Contracts.DTO.Response;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    [ServiceContract(CallbackContract = typeof(ILobbyManagerCallback))]
    public interface ILobbyManager
    {
        [OperationContract]
        Task<MatchCreationResponse> CreateLobby(MatchSettings settings);

        [OperationContract]
        Task<MatchJoinResponse> JoinLobby(JoinLobbyRequest request);

        [OperationContract]
        Task<bool> SendInvitations(string lobbyCode, string sender, List<string> guests);

        [OperationContract]
        void ConnectToLobby(string lobbyCode, string nickname);

        [OperationContract]
        void DisconnectFromLobby(string lobbyCode, string nickname);

        [OperationContract]
        void SetReadyStatus(string lobbyCode, string nickname, bool isReady);

        [OperationContract]
        void StartGame(string lobbyCode, int userId);

        [OperationContract]
        void KickPlayer(string lobbyCode, int hostUserId, string targetNickname);

        [OperationContract]
        Task<bool> SendLobbyInviteToFriend(string lobbyCode, string senderNickname, string targetUsername);

        [OperationContract]
        bool Ping();
    }

}
