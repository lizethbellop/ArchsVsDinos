using ArchsVsDinosServer.BusinessLogic.ProfileManagement;
using ArchsVsDinosServer.Utils;
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

        public static LobbyPlayerDTO FromLogin(UserAccountDTO userAccount, PlayerDTO player, bool isHost)
        {
            return new LobbyPlayerDTO
            {
                IdPlayer = player?.IdPlayer ?? 0,
                Username = userAccount.Username,
                Nickname = userAccount.Nickname,
                ProfilePicture = player?.ProfilePicture,
                IsHost = isHost
            };
        }

        public static LobbyPlayerDTO CreateRegisteredPlayer(string username, string nickname)
        {
            var profile = new ProfileInformation().GetPlayerByUsername(username);
            return new LobbyPlayerDTO
            {
                Username = username,
                Nickname = nickname,
                ProfilePicture = profile?.ProfilePicture,
                IsHost = false
            };
        }

        public static LobbyPlayerDTO CreateUnregisteredPlayer()
        {

            return new LobbyPlayerDTO
            {
                Username = UnregisteredPlayerGenerator.GenerateIdUnregisteredPlayer(),
                Nickname = UnregisteredPlayerGenerator.GenerateNicknameUnregisteredPlayer(),
                IsHost = false
            };
        }

    }
}
