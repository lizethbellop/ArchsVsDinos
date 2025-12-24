using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Result_Codes
{
    [DataContract]
    public enum UpdateResultCode
    {
        [EnumMember] Profile_Success,
        [EnumMember] Profile_ChangeNicknameSuccess,
        [EnumMember] Profile_ChangeUsernameSuccess,
        [EnumMember] Profile_ChangePasswordSuccess,
        [EnumMember] Profile_UpdateFacebookSuccess,
        [EnumMember] Profile_UpdateInstagramSuccess,
        [EnumMember] Profile_UpdateXSuccess,
        [EnumMember] Profile_UpdateTikTokSuccess,
        [EnumMember] Profile_EmptyFields,
        [EnumMember] Profile_UserNotFound,
        [EnumMember] Profile_PlayerNotFound,
        [EnumMember] Profile_NicknameExists,
        [EnumMember] Profile_UsernameExists,
        [EnumMember] Profile_PasswordTooShort,
        [EnumMember] Profile_InvalidPassword,
        [EnumMember] Profile_SamePasswordValue,
        [EnumMember] Profile_SameNicknameValue,
        [EnumMember] Profile_SameUsernameValue,
        [EnumMember] Profile_DatabaseError,
        [EnumMember] Profile_UnexpectedError,
        [EnumMember] Profile_PasswordTooLong,
        [EnumMember] Profile_PasswordNeedsLowercase,
        [EnumMember] Profile_PasswordNeedsUppercase,
        [EnumMember] Profile_PasswordNeedsNumber,
        [EnumMember] Profile_PasswordNeedsSpecialCharacter
    }
}
