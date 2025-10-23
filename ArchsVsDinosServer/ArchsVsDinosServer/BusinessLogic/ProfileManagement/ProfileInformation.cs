using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using ArchsVsDinosServer.Wrappers;
using Contracts.DTO;
using Contracts.DTO.Response;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts.DTO.Result_Codes;

namespace ArchsVsDinosServer.BusinessLogic.ProfileManagement
{
    public class ProfileInformation : BaseProfileService
    {
        public ProfileInformation(
        Func<IDbContext> contextFactory,
        IValidationHelper validationHelper,
        ILoggerHelper loggerHelper,
        ISecurityHelper securityHelper)
        : base(contextFactory, validationHelper, loggerHelper, securityHelper)
        {
        }

        public ProfileInformation()
        : base(
            () => new DbContextWrapper(),
            new ValidationHelperWrapper(),
            new LoggerHelperWrapper(),
            new SecurityHelperWrapper())
        {
        }

        public UpdateResponse UpdateNickname(string username, string newNickname)
        {
            try
            {
                var response = new UpdateResponse();

                if (UpdateIsEmpty(username, newNickname))
                {
                    response.success = false;
                    response.message = "Los campos son obligatorios";
                    response.resultCode = UpdateResultCode.Profile_EmptyFields;
                    return response;
                }

                using (var context = GetContext())
                {
                    var userAccount = context.UserAccount.FirstOrDefault(u => u.username == username);

                    if (userAccount == null)
                    {
                        response.success = false;
                        response.message = "Usuario no encontrado";
                        response.resultCode = UpdateResultCode.Profile_UserNotFound;
                        return response;
                    }

                    if (userAccount.nickname == newNickname)
                    {
                        response.success = false;
                        response.message = "El nuevo nickname debe ser diferente al actual";
                        response.resultCode = UpdateResultCode.Profile_SameNicknameValue;
                    }

                    if (context.UserAccount.Any(u => u.nickname == newNickname))
                    {
                        response.success = false;
                        response.message = "El nickname ya está en uso";
                        response.resultCode = UpdateResultCode.Profile_NicknameExists;
                        return response;
                    }

                    userAccount.nickname = newNickname;
                    context.SaveChanges();

                    response.success = true;
                    response.message = "Nickname actualizado exitosamente";
                    response.resultCode = UpdateResultCode.Profile_Success;
                    return response;
                }
            }
            catch (DbEntityValidationException ex)
            {
                loggerHelper.LogError("Error de validacion de base de datos del UpdateNickname", ex);
                return new UpdateResponse { success = false, message = $"Error: {ex.Message}", resultCode = UpdateResultCode.Profile_DatabaseError };
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error al actualizar el nickname: {ex.Message}", ex);
                return new UpdateResponse { success = false, message = $"Error: {ex.Message}", resultCode = UpdateResultCode.Profile_UnexpectedError };
            }
        }

        public UpdateResponse UpdateUsername(string currentUsername, string newUsername)
        {
            try
            {
                var response = new UpdateResponse();

                if (UpdateIsEmpty(currentUsername, newUsername))
                {
                    response.success = false;
                    response.message = "Los campos son obligatorios";
                    response.resultCode = UpdateResultCode.Profile_EmptyFields;
                    return response;
                }


                using (var context = GetContext())
                {
                    if (context.UserAccount.Any(u => u.username == newUsername))
                    {
                        response.success = false;
                        response.message = "El username ya está en uso";
                        response.resultCode = UpdateResultCode.Profile_UsernameExists;
                        return response;
                    }

                    var userAccount = context.UserAccount.FirstOrDefault(u => u.username == currentUsername);

                    if (userAccount == null)
                    {
                        response.success = false;
                        response.message = "Usuario no encontrado";
                        response.resultCode = UpdateResultCode.Profile_UserNotFound;
                        return response;
                    }

                    if (userAccount.username == newUsername)
                    {
                        response.success = false;
                        response.message = "El nuevo nickname debe ser diferente al actual";
                        response.resultCode = UpdateResultCode.Profile_SameUsernameValue;
                    }

                    userAccount.username = newUsername;
                    context.SaveChanges();

                    response.success = true;
                    response.message = "Username actualizado";
                    response.resultCode = UpdateResultCode.Profile_Success;

                    return response;
                }
            }
            catch (EntityException ex)
            {
                LoggerHelper.LogError($"Database connection error at Update Username", ex);
                return new UpdateResponse { success = false, message = $"Error: {ex.Message}", resultCode = UpdateResultCode.Profile_DatabaseError };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener el perfil: {ex.Message}");
                return new UpdateResponse { success = false, message = $"Error: {ex.Message}", resultCode = UpdateResultCode.Profile_UnexpectedError };
            }
        }

        public UpdateResponse ChangeProfilePicture(string username, byte[] profilePicture)
        {
            throw new NotImplementedException();
        }
    }
}
