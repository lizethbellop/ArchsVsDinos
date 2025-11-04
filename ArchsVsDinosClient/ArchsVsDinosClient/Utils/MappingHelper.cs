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
                idUser = source.IdUser,
                username = source.Username,
                name = source.Name,
                nickname = source.Nickname,
                email = source.Email
            };
        }

        public static PlayerDTO ToPlayerDTO(this AuthService.PlayerDTO source)
        {
            if (source == null) return null;

            return new PlayerDTO
            {
                idPlayer = source.IdPlayer,
                facebook = source.Facebook,
                instagram = source.Instagram,
                x = source.X,
                tiktok = source.Tiktok,
                totalWins = source.TotalWins,
                totalLosses = source.TotalLosses,
                totalPoints = source.TotalPoints,
                profilePicture = source.ProfilePicture
            };
        }
    }
}
