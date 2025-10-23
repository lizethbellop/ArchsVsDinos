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
    }
}
