using ArchsVsDinosClient.LobbyService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Services
{
    public class LobbyCallbackManager 
    {
        public event Action<LobbyPlayerDTO, string> OnCreatedMatch;

        public void LobbyCreated(LobbyPlayerDTO player, string lobbyId)
        {
            OnCreatedMatch?.Invoke(player, lobbyId);
        }
    }
}
