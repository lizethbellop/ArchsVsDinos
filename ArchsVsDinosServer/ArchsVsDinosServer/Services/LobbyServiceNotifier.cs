using ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Interfaces.Lobby;
using ArchsVsDinosServer.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    public class LobbyServiceNotifier : ILobbyServiceNotifier
    {
        private readonly ILobbyLogic lobbyLogic;
        private readonly LobbyCoreContext core;
        private readonly ILoggerHelper logger;

        public LobbyServiceNotifier(
            ILobbyLogic lobbyLogic,
            LobbyCoreContext core,
            ILoggerHelper logger)
        {
            this.lobbyLogic = lobbyLogic;
            this.core = core;
            this.logger = logger;
        }

        public void NotifyPlayerExpelled(string lobbyCode, int userId, string reason)
        {
            var lobby = core.Session.GetLobby(lobbyCode);
            if (lobby == null)
                return;

            var player = lobby.Players.FirstOrDefault(p => p.UserId == userId);
            if (player == null)
                return;

            logger.LogWarning(
                $"Player {player.Nickname} expelled from lobby {lobbyCode}. Reason: {reason}"
            );

            lobbyLogic.DisconnectPlayer(lobbyCode, player.Nickname);
        }

        public void NotifyLobbyClosure(string lobbyCode, string reason)
        {
            logger.LogWarning($"Lobby {lobbyCode} closed. Reason: {reason}");
            core.Session.RemoveLobby(lobbyCode);
        }
    }
}
