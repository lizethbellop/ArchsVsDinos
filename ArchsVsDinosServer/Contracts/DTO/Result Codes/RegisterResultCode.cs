using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Result_Codes
{
    [DataContract]
    public enum RegisterResultCode
    {
        [EnumMember] Register_Success,
        [EnumMember] Register_UsernameExists,
        [EnumMember] Register_NicknameExists,
        [EnumMember] Register_BothExists,
        [EnumMember] Register_DatabaseError,
        [EnumMember] Register_InvalidCode,
        [EnumMember] Register_UnexpectedError
    }
}
