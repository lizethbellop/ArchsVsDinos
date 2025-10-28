using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchsVsDinosClient.DTO;
using AuthService = ArchsVsDinosClient.AuthenticationService;

namespace ArchsVsDinosClient.Utils
{
    public static class MappingHelper
    {
        public static UserDTO ToUserDTO(this AuthService.UserDTO source)
        {
            if (source == null) return null;

            return new UserDTO
            {
                idUser = source.idUser,
                username = source.username,
                name = source.name,
                nickname = source.nickname,
                email = source.email
            };
        }

        public static PlayerDTO ToPlayerDTO(this AuthService.PlayerDTO source)
        {
            if (source == null) return null;

            return new PlayerDTO
            {
                idPlayer = source.idPlayer,
                facebook = source.facebook,
                instagram = source.instagram,
                x = source.x,
                tiktok = source.tiktok,
                totalWins = source.totalWins,
                totalLosses = source.totalLosses,
                totalPoints = source.totalPoints,
                profilePicture = source.profilePicture
            };
        }
    }
}
