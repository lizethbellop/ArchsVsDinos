using Contracts;
using Contracts.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement
{
    internal class MatchLobby
    {
        public string matchCode { get; set; }
        public List<LobbyPlayerDTO> players { get; set; } = new List<LobbyPlayerDTO>();
        public IMatchLobbyManagerCallback matchLobbyCallback { get; set; }

        private const int MaxPlayers = 4;

        public bool AddPlayer(LobbyPlayerDTO player)
        {
            if (players.Count >= MaxPlayers)
            {
                return false;
            }
            else
            {
                players.Add(player);
                return true;
            }

        }
    }
}
