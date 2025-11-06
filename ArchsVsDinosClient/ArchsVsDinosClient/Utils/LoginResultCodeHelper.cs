using ArchsVsDinosClient.AuthenticationService;
using ArchsVsDinosClient.Properties.Langs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Utils
{
    public static class LoginResultCodeHelper
    {
        public static string GetMessage(LoginResultCode resultCode)
        {
            switch (resultCode)
            {
                case LoginResultCode.Authentication_Success:
                    return Lang.Authentication_Success;

                case LoginResultCode.Authentication_EmptyFields:
                    return Lang.Authentication_EmptyFields;

                case LoginResultCode.Authentication_InvalidCredentials:
                    return Lang.Authentication_InvalidCredentials;

                case LoginResultCode.Authentication_DatabaseError:
                    return Lang.Authentication_DatabaseError;

                case LoginResultCode.Authentication_UnexpectedError:
                    return Lang.Authentication_UnexpectedError;

                case LoginResultCode.Authentication_UserBanned:
                    return Lang.Authentication_UserBanned;

                default:
                    return Lang.Authentication_UnexpectedError;
            }
        }
    }
}
