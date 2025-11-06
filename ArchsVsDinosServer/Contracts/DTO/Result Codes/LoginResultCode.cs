using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Result_Codes
{
    [DataContract]
    public enum LoginResultCode
    {
        [EnumMember] Authentication_Success,
        [EnumMember] Authentication_EmptyFields,
        [EnumMember] Authentication_InvalidCredentials,
        [EnumMember] Authentication_DatabaseError,
        [EnumMember] Authentication_UnexpectedError,
        [EnumMember] Authentication_UserBanned
    }
}
