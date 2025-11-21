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
                IdUser = source.IdUser,
                Username = source.Username,
                Name = source.Name,
                Nickname = source.Nickname,
                Email = source.Email
            };
        }

        public static PlayerDTO ToPlayerDTO(this AuthService.PlayerDTO source)
        {
            if (source == null) return null;

            return new PlayerDTO
            {
                IdPlayer = source.IdPlayer,
                Facebook = source.Facebook,
                Instagram = source.Instagram,
                X = source.X,
                Tiktok = source.Tiktok,
                TotalWins = source.TotalWins,
                TotalLosses = source.TotalLosses,
                TotalPoints = source.TotalPoints,
                ProfilePicture = source.ProfilePicture
            };
        }

    }
}
