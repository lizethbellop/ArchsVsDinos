using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using ArchsVsDinosServer.Wrappers;
using Contracts.DTO.Response;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.ProfileManagement
{
    public class SocialMediaManager : BaseProfileService
    {
        public SocialMediaManager(ServiceDependencies dependencies)
        : base(dependencies)
        {
        }

        public SocialMediaManager()
            : base(new ServiceDependencies())
        {
        }

        private UpdateResponse UpdateSocialMedia(string username, string link, string platform, UpdateResultCode successCode)
        {
            try
            {
                var response = new UpdateResponse();

                if (UpdateIsEmpty(username, link))
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

                    var player = context.Player.FirstOrDefault(p => p.idPlayer == userAccount.idPlayer);

                    if (player == null)
                    {
                        response.success = false;
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
                    response.resultCode = successCode;
                    return response;
                }
            }
            catch (DbEntityValidationException ex)
            {
                loggerHelper.LogError($"Database validation error at Update {platform}", ex);
                return new UpdateResponse { success = false, resultCode = UpdateResultCode.Profile_DatabaseError };
            }
            catch (Exception ex)
            {
                return new UpdateResponse { success = false, resultCode = UpdateResultCode.Profile_UnexpectedError };
            }
        }

        public UpdateResponse UpdateFacebook(string username, string newFacebook)
        {
            return UpdateSocialMedia(username, newFacebook, "Facebook", UpdateResultCode.Profile_UpdateFacebookSuccess);

        }

        public UpdateResponse UpdateInstagram(string username, string newInstagram)
        {
            return UpdateSocialMedia(username, newInstagram, "Instagram", UpdateResultCode.Profile_UpdateInstagramSuccess);
        }



        public UpdateResponse UpdateTikTok(string username, string newTikTok)
        {
            return UpdateSocialMedia(username, newTikTok,"TikTok", UpdateResultCode.Profile_UpdateTikTokSuccess);
        }



        public UpdateResponse UpdateX(string username, string newX)
        {
            return UpdateSocialMedia(username, newX, "X", UpdateResultCode.Profile_UpdateXSuccess);
        }


    }
}
