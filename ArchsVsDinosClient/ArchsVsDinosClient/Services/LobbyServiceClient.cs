using ArchsVsDinosClient.AuthenticationService;
using ArchsVsDinosClient.LobbyService;
using ArchsVsDinosClient.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services
{
    public class LobbyServiceClient : ILobbyServiceClient
    {
        private readonly LobbyManagerClient lobbyManagerClient;
        private readonly LobbyCallbackManager callbackManager;

        public event Action<LobbyPlayerDTO, string> LobbyCreated;
        public event Action<LobbyPlayerDTO> PlayerJoined;
        public event Action<LobbyPlayerDTO> PlayerLeft;
        public event Action<LobbyPlayerDTO> PlayerExpelled;
        public event Action<string> LobbyCancelled;

        public LobbyServiceClient()
        { 
            callbackManager = new LobbyCallbackManager();

            callbackManager.OnCreatedLobby += (player, code) => LobbyCreated?.Invoke(player, code);
            callbackManager.OnJoinedLobby += (player) => PlayerJoined?.Invoke(player);
            callbackManager.OnPlayerLeftLobby += (player) => PlayerLeft?.Invoke(player);
            callbackManager.OnPlayerExpelled += (player) => PlayerExpelled?.Invoke(player);
            callbackManager.OnLobbyCancelled += (code) => LobbyCancelled?.Invoke(code);

            var context = new InstanceContext(callbackManager);
            lobbyManagerClient = new LobbyManagerClient(context);
        }

        public void CreateLobby(UserAccountDTO userAccount)
        {
            lobbyManagerClient.CreateLobby(userAccount);
        }

        public LobbyResultCode JoinLobby(UserAccountDTO userAccount, string matchCode)
        {
            return lobbyManagerClient.JoinLobby(userAccount, matchCode);
        }

        public void LeaveLobby(string username)
        {
            lobbyManagerClient.LeaveLobby(username);
        }

        public void ExpelPlayer(string hostUsername, string targetUsername)
        {
            lobbyManagerClient.ExpelPlayerLobby(hostUsername, targetUsername);
        }
        public void CancellLobby(string matchCode, string usernameRequester)
        {
            lobbyManagerClient.CancelLobby(matchCode, usernameRequester);
        }

    }
}
