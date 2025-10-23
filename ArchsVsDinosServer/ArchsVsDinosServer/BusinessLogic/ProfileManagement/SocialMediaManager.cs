using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Wrappers;
using Contracts.DTO.Response;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts.DTO.Result_Codes;

namespace ArchsVsDinosServer.BusinessLogic.ProfileManagement
{
    public class SocialMediaManager : BaseProfileService
    {
        public SocialMediaManager(
        Func<IDbContext> contextFactory,
        IValidationHelper validationHelper,
        ILoggerHelper loggerHelper,
        ISecurityHelper securityHelper)
        : base(contextFactory, validationHelper, loggerHelper, securityHelper)
        {
        }

        public SocialMediaManager()
        : base(
            () => new DbContextWrapper(),
            new ValidationHelperWrapper(),
            new LoggerHelperWrapper(),
            new SecurityHelperWrapper())
        {
        }

        private UpdateResponse UpdateSocialMedia(string username, string link, string platform)
        {
            try
            {
                var response = new UpdateResponse();

                if (UpdateIsEmpty(username, link))
                {
                    response.success = false;
                    response.message = "Los campos son requeridos";
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

                    var player = context.Player.FirstOrDefault(p => p.idPlayer == userAccount.idPlayer);

                    if (player == null)
                    {
                        response.success = false;
                        response.message = "Perfil de jugador no encontrado";
                        response.resultCode = UpdateResultCode.Profile_PlayerNotFound;
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

                    response.success = true;
                    response.message = $"{platform} actualizado exitosamente";
                    response.resultCode = UpdateResultCode.Profile_Success;
                    return response;
                }
            }
            catch (DbEntityValidationException ex)
            {
                loggerHelper.LogError($"Database validation error at Update {platform}", ex);
                return new UpdateResponse { success = false, message = "Error en la base de datos",resultCode = UpdateResultCode.Profile_DatabaseError };
            }
            catch (Exception ex)
            {
                return new UpdateResponse { success = false, message = $"{ex.Message}", resultCode = UpdateResultCode.Profile_UnexpectedError };
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



        public UpdateResponse UpdateTikTok(string username, string newTikTok)
        {
            return UpdateSocialMedia(username, newTikTok, "TikTok");
        }



        public UpdateResponse UpdateX(string username, string newX)
        {
            return UpdateSocialMedia(username, newX, "X");
        }


    }
}
