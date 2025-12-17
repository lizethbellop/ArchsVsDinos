using Contracts.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Model
{
    public class ActiveLobbyData
    {
        public string LobbyCode { get; set; }
        public MatchSettings Settings { get; set; }

        public List<LobbyPlayer> Players { get; set; }

        public readonly object LobbyLock = new object();

        public ActiveLobbyData(string lobbyCode, MatchSettings settings)
        {
            LobbyCode = lobbyCode;
            Settings = settings;
            Players = new List<LobbyPlayer>();
            Players.Add(new LobbyPlayer(0, settings.HostNickname)
            {
                IsReady = true 
            });
        }

        public ActiveLobbyData() { }

        public bool AddPlayer(int userId, string nickname)
        {
            lock (LobbyLock)
            {
                if (Players.Count >= Settings.MaxPlayers)
                    return false;

                if (Players.Any(p =>
                    p.UserId == userId ||
                    p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase)))
                    return false;

                Players.Add(new LobbyPlayer(userId, nickname));
                return true;
            }
        }

        public void RemovePlayer(string nickname)
        {
            lock (LobbyLock)
            {
                var player = Players
                    .FirstOrDefault(p => p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));

                if (player != null)
                    Players.Remove(player);
            }
        }
    }

}
