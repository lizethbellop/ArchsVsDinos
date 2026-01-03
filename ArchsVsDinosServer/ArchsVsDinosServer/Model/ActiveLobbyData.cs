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

        public int HostUserId { get; set; }

        public ActiveLobbyData(string lobbyCode, MatchSettings settings)
        {
            LobbyCode = lobbyCode;
            Settings = settings;
            Players = new List<LobbyPlayer>();
            LobbyLock = new object();

            HostUserId = settings.HostUserId;
        }

        public ActiveLobbyData() { }

        public bool AddPlayer(int userId, string username, string nickname)
        {
            lock (LobbyLock)
            {
                if (Players.Count >= Settings.MaxPlayers)
                    return false;

                bool alreadyInLobby = Players.Any(player =>
                {
                    if (player == null) return false;

                    if (player.UserId == userId) return true;

                    if (!string.IsNullOrEmpty(nickname) &&
                        nickname.Equals(player.Nickname, StringComparison.OrdinalIgnoreCase))
                        return true;

                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(player.Username))
                    {
                        if (username.Equals(player.Username, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }

                    return false;
                });

                if (alreadyInLobby)
                    return false;

                Players.Add(new LobbyPlayer(userId, username, nickname));
                return true;
            }
        }

        public void TransferHostToNextPlayer()
        {
            if (Players.Count > 0)
            {
                var newHost = Players.FirstOrDefault(player => player.UserId != HostUserId)
                              ?? Players.First();
                HostUserId = newHost.UserId;
            }
        }

        public void RemovePlayer(string nickname)
        {
            lock (LobbyLock)
            {
                var player = Players
                    .FirstOrDefault(playerSelected => playerSelected.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));

                if (player != null)
                    Players.Remove(player);
            }
        }
    }

}
