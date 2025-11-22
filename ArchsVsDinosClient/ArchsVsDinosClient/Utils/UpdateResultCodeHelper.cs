using ArchsVsDinosClient.ProfileManagerService;
using ArchsVsDinosClient.Properties.Langs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Utils
{
    public static class UpdateResultCodeHelper
    {
        public static string GetMessage(UpdateResultCode resultCode)
        {
            switch (resultCode)
            {
                case UpdateResultCode.Profile_Success:
                    return Lang.Profile_Success;

                case UpdateResultCode.Profile_ChangeNicknameSuccess:
                    return Lang.Profile_ChangeNicknameSuccess;

                case UpdateResultCode.Profile_ChangeUsernameSuccess:
                    return Lang.Profile_ChangeUsernameSuccess;

                case UpdateResultCode.Profile_ChangePasswordSuccess:
                    return Lang.Profile_ChangePasswordSuccess;

                case UpdateResultCode.Profile_UpdateFacebookSuccess:
                    return Lang.Profile_UpdateFacebookSuccess;

                case UpdateResultCode.Profile_UpdateInstagramSuccess:
                    return Lang.Profile_UpdateInstagramSuccess;

                case UpdateResultCode.Profile_UpdateXSuccess:
                    return Lang.Profile_UpdateXSuccess;

                case UpdateResultCode.Profile_UpdateTikTokSuccess:
                    return Lang.Profile_UpdateTikTokSuccess;

                case UpdateResultCode.Profile_EmptyFields:
                    return Lang.Profile_EmptyFields;

                case UpdateResultCode.Profile_UserNotFound:
                    return Lang.Profile_UserNotFound;

                case UpdateResultCode.Profile_PlayerNotFound:
                    return Lang.Profile_PlayerNotFound;

                case UpdateResultCode.Profile_NicknameExists:
                    return Lang.Profile_NicknameExists;

                case UpdateResultCode.Profile_UsernameExists:
                    return Lang.Profile_UsernameExists;

                case UpdateResultCode.Profile_PasswordTooShort:
                    return Lang.Profile_PasswordTooShort;

                case UpdateResultCode.Profile_InvalidPassword:
                    return Lang.Profile_InvalidPassword;

                case UpdateResultCode.Profile_SamePasswordValue:
                    return Lang.Profile_SamePasswordValue;

                case UpdateResultCode.Profile_SameNicknameValue:
                    return Lang.Profile_SameNicknameValue;

                case UpdateResultCode.Profile_SameUsernameValue:
                    return Lang.Profile_SameUsernameValue;

                case UpdateResultCode.Profile_DatabaseError:
                    return Lang.Profile_DatabaseError;

                case UpdateResultCode.Profile_UnexpectedError:
                    return Lang.Profile_UnexpectedError;


                default:
                    return Lang.Profile_UnexpectedError;
            }
        }
    }
}
