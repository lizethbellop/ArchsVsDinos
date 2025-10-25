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
using System.IO;
using System.Data.Entity;

namespace ArchsVsDinosServer.BusinessLogic.ProfileManagement
{
    public class ProfileInformation : BaseProfileService
    {
        public ProfileInformation(ServiceDependencies dependencies)
        : base(dependencies)
        {
        }

        public ProfileInformation()
            : base(new ServiceDependencies())
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
                    response.resultCode = UpdateResultCode.Profile_EmptyFields;
                    return response;
                }

                using (var context = GetContext())
                {
                    var userAccount = context.UserAccount.FirstOrDefault(u => u.username == username);

                    if (userAccount == null)
                    {
                        response.success = false;
                        response.resultCode = UpdateResultCode.Profile_UserNotFound;
                        return response;
                    }

                    if (userAccount.nickname == newNickname)
                    {
                        response.success = false;
                        response.resultCode = UpdateResultCode.Profile_SameNicknameValue;
                        return response;
                    }

                    if (context.UserAccount.Any(u => u.nickname == newNickname))
                    {
                        response.success = false;
                        response.resultCode = UpdateResultCode.Profile_NicknameExists;
                        return response;
                    }

                    userAccount.nickname = newNickname;
                    context.SaveChanges();

                    response.success = true;
                    response.resultCode = UpdateResultCode.Profile_ChangeNicknameSuccess;
                    return response;
                }
            }
            catch (DbEntityValidationException ex)
            {
                loggerHelper.LogError("Error de validacion de base de datos del UpdateNickname", ex);
                return new UpdateResponse { success = false, resultCode = UpdateResultCode.Profile_DatabaseError };
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error al actualizar el nickname: {ex.Message}", ex);
                return new UpdateResponse { success = false, resultCode = UpdateResultCode.Profile_UnexpectedError };
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
                    response.resultCode = UpdateResultCode.Profile_EmptyFields;
                    return response;
                }


                using (var context = GetContext())
                {
                    if (context.UserAccount.Any(u => u.username == newUsername))
                    {
                        response.success = false;
                        response.resultCode = UpdateResultCode.Profile_UsernameExists;
                        return response;
                    }

                    var userAccount = context.UserAccount.FirstOrDefault(u => u.username == currentUsername);

                    if (userAccount == null)
                    {
                        response.success = false;
                        response.resultCode = UpdateResultCode.Profile_UserNotFound;
                        return response;
                    }

                    if (userAccount.username == newUsername)
                    {
                        response.success = false;
                        response.resultCode = UpdateResultCode.Profile_SameUsernameValue;
                        return response;
                    }

                    userAccount.username = newUsername;
                    context.SaveChanges();

                    response.success = true;
                    response.resultCode = UpdateResultCode.Profile_ChangeUsernameSuccess;

                    return response;
                }
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError($"Database connection error at Update Username", ex);
                return new UpdateResponse { success = false, resultCode = UpdateResultCode.Profile_DatabaseError };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener el perfil: {ex.Message}");
                return new UpdateResponse { success = false, resultCode = UpdateResultCode.Profile_UnexpectedError };
            }
        }

        public UpdateResponse ChangeProfilePicture(string username, byte[] profilePicture, string fileExtension)
        {
            try
            {
                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProfilePictures");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string fileName = $"{username}_{Guid.NewGuid()}{fileExtension}";
                string filePath = Path.Combine(folderPath, fileName);

                File.WriteAllBytes(filePath, profilePicture);

                using (var context = GetContext())
                {
                    UpdateResponse response = new UpdateResponse();

                    UserAccount user = context.UserAccount.FirstOrDefault(u => u.username == username);
                    if (user == null)
                    {
                        response.success = false;
                        response.resultCode = UpdateResultCode.Profile_UserNotFound;
                        return response;
                    }

                    Player player = context.Player.FirstOrDefault(p => p.idPlayer == user.idPlayer);
                    if (player == null)
                    {
                        response.success = false;
                        response.resultCode = UpdateResultCode.Profile_PlayerNotFound;
                        return response;
                    }

                    player.profilePicture = fileName;
                    context.SaveChanges();

                    response.success = true;
                    response.resultCode = UpdateResultCode.Profile_Success;
                    return response;

                }
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError($"Database connection error at Change Profile Picture", ex);
                return new UpdateResponse { success = false, resultCode = UpdateResultCode.Profile_DatabaseError };
            }
            catch (IOException ex)
            {
                loggerHelper.LogError($"An error with the file happened", ex);
                return new UpdateResponse { success = false, resultCode =UpdateResultCode.Profile_DatabaseError };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while changing the profile picture: {ex.Message}");
                return new UpdateResponse { success = false, resultCode = UpdateResultCode.Profile_UnexpectedError };
            }
        }

        public byte[] GetProfilePicture(string username)
        {
            try
            {
                using (var context = GetContext())
                {
                    var user = context.UserAccount.FirstOrDefault(u => u.username == username);

                    if (user == null)
                        return null;

                    var player = context.Player.FirstOrDefault(p => p.idPlayer == user.idPlayer);

                    if (player == null || string.IsNullOrEmpty(player.profilePicture))
                        return null;


                    string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProfilePictures");
                    string filePath = Path.Combine(folderPath, player.profilePicture);

                    if (!File.Exists(filePath))
                        return null;

                    return File.ReadAllBytes(filePath);
                }
            }
            catch (IOException ex)
            {
                loggerHelper.LogError($"An error with the file happened", ex);
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                loggerHelper.LogError($"An authorization error happened", ex);
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
