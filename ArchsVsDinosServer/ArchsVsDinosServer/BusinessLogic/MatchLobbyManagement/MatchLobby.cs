using Contracts;
using Contracts.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement
{
    internal class Lobby
    {
        public string MatchCode { get; set; }
        public List<LobbyPlayerDTO> Players { get; set; } = new List<LobbyPlayerDTO>();

        public List<ILobbyManagerCallback> Callbacks { get; private set; } = new List<ILobbyManagerCallback>();

        private const int MaxPlayers = 4;

        public void AddCallback(ILobbyManagerCallback callback)
        {
            lock (Callbacks)
            {
                Callbacks.Add(callback);
            }
        }

        public bool AddPlayer(LobbyPlayerDTO player)
        {
            if (Players.Count >= MaxPlayers)
            {
                return false;
            }
            else
            {
                Players.Add(player);
                return true;
            }

        }
    }
}
