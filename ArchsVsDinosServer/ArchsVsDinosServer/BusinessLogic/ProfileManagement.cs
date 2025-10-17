using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using ArchsVsDinosServer.Wrappers;
using Contracts;
using Contracts.DTO;
using Contracts.DTO.Response;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic
{
    public class ProfileManagement
    {

        private readonly IValidationHelper validationHelper;
        private readonly Func<IDbContext> contextFactory;
        private readonly ILoggerHelper loggerHelper;
        private readonly ISecurityHelper securityHelper;

        public ProfileManagement(Func<IDbContext> _contextFactory, IValidationHelper _validationHelper, ILoggerHelper _loggerHelper, ISecurityHelper _securityHelper)
        {
            validationHelper = _validationHelper;
            contextFactory = _contextFactory;
            loggerHelper = _loggerHelper;
            securityHelper = _securityHelper;
        }

        public ProfileManagement() : this(() => new DbContextWrapper(), 
            new Wrappers.ValidationHelperWrapper(), new Wrappers.LoggerHelperWrapper(), new Wrappers.SecurityHelperWrapper())
        {

        }


        private IDbContext GetContext()
        {
            return contextFactory();
        }
        public UpdateResponse ChangePassword(string username, string currentPassword, string newPassword)
        {
            try
            {
                var response = new UpdateResponse();

                if(ChangePasswordIsEmpty(username, currentPassword, newPassword))
                { 
                    response.Success = false;
                    response.Message = "Todos los campos son obligatorios";
                    return response;
                }

                if(currentPassword == newPassword)
                {
                    response.Success = false;
                    response.Message = "La nueva contraseña debe ser diferente a la actual";
                    return response;
                }

                if(newPassword.Length < 8)
                {
                    response.Success = false;
                    response.Message = "La nueva contraseña debe tener al menos 8 caracteres";
                    return response;
                }

                using (var context = GetContext())
                {
                    var userAccount = context.UserAccount.FirstOrDefault(u => u.username == username);

                    if(userAccount == null)
                    {
                        response.Success = false;
                        response.Message = "Usuario no encontrado";
                        return response;
                    }

                    if(!VerifyPassword(currentPassword, userAccount.password))
                    {
                        response.Success = false;
                        response.Message = "La contraseña actual es incorrecta";
                        return response;
                    }

                    userAccount.password = securityHelper.HashPassword(newPassword);
                    context.SaveChanges();

                    response.Success = true;
                    response.Message = "Contraseña actualizada exitosamente";
                    return response;


                }
            }
            catch (DbEntityValidationException e)
            {
                loggerHelper.LogError("Error de validacion en el metodo ChangePassword", e);
                return new UpdateResponse { Success = false, Message = "Error en la base de datos" };
            }
            catch (Exception e)
            {
                return new UpdateResponse { Success = false, Message = $"Error: {e.Message}" };
            }
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

        public UpdateResponse UpdateFacebook(string username, string newFacebook)
        {
            return UpdateSocialMedia(username, newFacebook, "Facebook");
        }

        public UpdateResponse UpdateInstagram(string username, string newInstagram)
        {
            return UpdateSocialMedia(username, newInstagram, "Instagram");
        }

        public UpdateResponse UpdateNickname(string username, string newNickname)
        {
            try
            {
                var response = new UpdateResponse();

                if(UpdateIsEmpty(username, newNickname))
                {
                    response.Success = false;
                    response.Message = "Los campos son obligatorios";
                    return response;
                }

                using (var context = GetContext())
                {
                    var userAccount = context.UserAccount.FirstOrDefault(u => u.username == username);

                    if(userAccount == null)
                    {
                        response.Success = false;
                        response.Message = "Usuario no encontrado";
                        return response;
                    }

                    userAccount.nickname = newNickname;
                    context.SaveChanges();

                    response.Success = true;
                    response.Message = "Nickname actualizado exitosamente";
                    return response;
                }
            }
            catch (DbEntityValidationException ex)
            {
                loggerHelper.LogError("Error de validacion de base de datos del UpdateNickname", ex);
                return new UpdateResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error al actualizar el nickname: {ex.Message}", ex);
                return new UpdateResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public UpdateResponse UpdateTikTok(string username, string newTikTok)
        {
            return UpdateSocialMedia(username, newTikTok, "TikTok");
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
                return new UpdateResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener el perfil: {ex.Message}");
                return new UpdateResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public UpdateResponse UpdateX(string username, string newX)
        {
            return UpdateSocialMedia(username, newX, "X");
        }


        private bool IsEmpty(string value)
        {
            return validationHelper.IsEmpty(value);
        }

        private bool UpdateIsEmpty(string value1, string value2)
        {
            return validationHelper.IsEmpty(value1) || validationHelper.IsEmpty(value2); 
        }

        private bool ChangePasswordIsEmpty(string value1, string value2, string value3)
        {
            return validationHelper.IsEmpty(value1) || validationHelper.IsEmpty(value2) || validationHelper.IsEmpty(value3);
        }

        private bool VerifyPassword(string plainPassword, string hashedPassword)
        {
            return securityHelper.HashPassword(plainPassword) == hashedPassword;
        }

        private UpdateResponse UpdateSocialMedia(string username, string link, string platform)
        {
            try
            {
                var response = new UpdateResponse();

                if (UpdateIsEmpty(username, link))
                {
                    response.Success = false;
                    response.Message = "Los campos son requeridos";
                    return response;
                }

                using (var context = GetContext())
                {
                    var userAccount = context.UserAccount.FirstOrDefault(u => u.username == username);

                    if (userAccount == null)
                    {
                        response.Success = false;
                        response.Message = "Usuario no encontrado";
                        return response;
                    }

                    var player = context.Player.FirstOrDefault(p => p.idPlayer == userAccount.idPlayer);

                    if (player == null)
                    {
                        response.Success = false;
                        response.Message = "Perfil de jugador no encontrado";
                        return response;
                    }

                    switch (platform)
                    {
                        case "Facebook":
                            player.facebook = link;
                            break;
                        case "X":
                            player.x = link;
                            break;
                        case "Instagram":
                            player.instagram = link;
                            break;
                        case "TikTok":
                            player.tiktok = link;
                            break;
                    }

                    context.SaveChanges();

                    response.Success = true;
                    response.Message = $"{platform} actualizado exitosamente";
                    return response;
                }
            }
            catch (DbEntityValidationException ex)
            {
                loggerHelper.LogError($"Database validation error at Update {platform}", ex);
                return new UpdateResponse { Success = false, Message = "Error en la base de datos" };
            }
            catch(Exception ex)
            {
                return new UpdateResponse { Success = false, Message = $"{ex.Message}" };
            }
        }
    }
}
