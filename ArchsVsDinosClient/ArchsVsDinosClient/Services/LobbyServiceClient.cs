using ArchsVsDinosClient.AuthenticationService;
using ArchsVsDinosClient.LobbyService;
using ArchsVsDinosClient.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services
{
    public class LobbyServiceClient : ILobbyServiceClient
    {
        private readonly LobbyManagerClient client;

        public event Action<LobbyPlayerDTO, string> LobbyCreated;

        public LobbyServiceClient()
        {
            var callback = new LobbyCallbackManager();
            callback.OnCreatedMatch += (player, lobbyId) =>
            {
                LobbyCreated?.Invoke(player, lobbyId);
            };

            var context = new InstanceContext(callback);
            client = new LobbyManagerClient(context);
        }

        public void CreateLobby(UserAccountDTO userAccount)
        {
            client.CreateLobby(userAccount);
        }

        private PlayerDTO GetPlayerById(int idPlayer)
        {
            return new PlayerDTO
            {
                IdPlayer = idPlayer,
                ProfilePicture = "default.png"
            };
        }

    }
}
