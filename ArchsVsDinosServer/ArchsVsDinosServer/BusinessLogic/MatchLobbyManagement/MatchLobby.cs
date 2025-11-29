using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using ArchsVsDinosServer.Wrappers;
using Contracts;
using Contracts.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement
{
    public class Lobby
    {
        public string MatchCode { get; set; }
        public List<LobbyPlayerDTO> Players { get; set; } = new List<LobbyPlayerDTO>();
        public List<ILobbyManagerCallback> Callbacks { get; private set; } = new List<ILobbyManagerCallback>();
        public Dictionary<ILobbyManagerCallback, string> CallbackOwners { get; private set; }
            = new Dictionary<ILobbyManagerCallback, string>();
        private const int MaxPlayers = 4;

        public void AddCallback(ILobbyManagerCallback callback, string username)
        {
            lock (Callbacks)
            {
                Callbacks.Add(callback);
                CallbackOwners[callback] = username;
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
