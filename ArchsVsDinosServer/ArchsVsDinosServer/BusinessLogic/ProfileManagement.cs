using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts;
using Contracts.DTO;
using Contracts.DTO.Response;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic
{
    public class ProfileManagement : IProfileManager
    {

        private readonly IValidationHelper validationHelper;

        public ProfileManagement(IValidationHelper _validationHelper)
        {
            validationHelper = _validationHelper;
        }

        private ArchsVsDinosConnection GetContext()
        {
            return new ArchsVsDinosConnection();
        }
        public bool ChangePassword(string username, string currentPassword, string newPassword)
        {
            throw new NotImplementedException();
        }

        public bool ChangeProfilePicture(string username)
        {
            throw new NotImplementedException();
        }

        public PlayerDTO GetProfile(string username)
        {
            try
            {
                using (var context = GetContext())
                {
                    var userAccount = context.UserAccount.FirstOrDefault(u => u.username == username);

                    if(userAccount == null)
                    {
                        return null;
                    }

                    var player = context.Player.FirstOrDefault(p => p.idPlayer == userAccount.idPlayer);

                    if(player == null)
                    {
                        return null;
                    }

                    return new PlayerDTO
                    {
                        idPlayer = player.idPlayer,
                        facebook = player.facebook,
                        instagram = player.instagram,
                        x = player.x,
                        tiktok = player.tiktok,
                        totalWins = player.totalWins,
                        totalLosses = player.totalLosses,
                        totalPoints = player.totalPoints,
                        profilePicture = player.profilePicture
                    };
                }
            }
            catch (EntityException ex)
            {
                LoggerHelper.LogError($"Database connection error at Get Profile for user: {username}", ex);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener el perfil: {ex.Message}");
                return null;
            }
        }

        public bool UpdateFacebook(string username, string newFacebook)
        {
            throw new NotImplementedException();
        }

        public bool UpdateInstagram(string username, string newInstagram)
        {
            throw new NotImplementedException();
        }

        public bool UpdateNickname(string username, string newNickname)
        {
            throw new NotImplementedException();
        }

        public bool UpdateTikTok(string username, string newTikTok)
        {
            throw new NotImplementedException();
        }

        public UpdateResponse UpdateUsername(string currentUsername, string newUsername)
        {
            try
            {
                var response = new UpdateResponse();
                
                if(UpdateIsEmpty(currentUsername, newUsername)){
                    response.Success = false;
                    response.Message = "Los campos son obligatorios";
                    return response;
                }

                if(currentUsername == newUsername)
                {
                    response.Success = false;
                    response.Message = "El nuevo username debe ser diferente al actual";
                    return response;
                }
                
                using (var context = GetContext())
                {
                    if(context.UserAccount.Any(u => u.username == newUsername))
                    {
                        response.Success=false;
                        response.Message = "El username ya está en uso";
                        return response;
                    }

                    var userAccount = context.UserAccount.FirstOrDefault(u => u.username == currentUsername);

                    if (userAccount == null)
                    {
                        response.Success = false;
                        response.Message = "Usuario no encontrado";
                        return response;
                    }

                    userAccount.username = newUsername;
                    context.SaveChanges();

                    response.Success = true;
                    response.Message = "Username actualizado";

                    return response;
                }
            }
            catch (EntityException ex)
            {
                LoggerHelper.LogError($"Database connection error at Update Username", ex);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener el perfil: {ex.Message}");
                return null;
            }
        }

        public bool UpdateX(string username, string newX)
        {
            throw new NotImplementedException();
        }


        private bool IsEmpty(string value)
        {
            return validationHelper.IsEmpty(value);
        }

        private bool UpdateIsEmpty(string value1, string value2)
        {
            return validationHelper.IsEmpty(value1) || validationHelper.IsEmpty(value2); 
        }
    }
}
