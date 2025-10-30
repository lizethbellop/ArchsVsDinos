using ArchsVsDinosServer.BusinessLogic.ProfileManagement;
using Contracts.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement
{
    public static class PlayerCreator
    {

        public static LobbyPlayerDTO CreateHostPlayer(string username, string nickname)
        {
            var profile = new ProfileInformation().GetPlayerByUsername(username);
            return new LobbyPlayerDTO
            {
                username = username,
                nickname = nickname,
                profilePicture = profile?.profilePicture,
                isHost = true
            };
        }

        public static LobbyPlayerDTO CreateregisteredPlayer(string username, string nickname)
        {
            var profile = new ProfileInformation().GetPlayerByUsername(username);
            return new LobbyPlayerDTO
            {
                username = username,
                nickname = nickname,
                profilePicture = profile?.profilePicture,
                isHost = false
            };
        }

    }
}
