using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Result_Codes
{
    [DataContract]
    public enum PasswordRecoveryResult
    {
        [EnumMember]
        PasswordRecovery_Success,       

        [EnumMember]
        PasswordRecovery_UserNotFound,

        [EnumMember]
        PasswordRecovery_CooldownActive,

        [EnumMember]
        PasswordRecovery_InvalidCode,

        [EnumMember]
        PasswordRecovery_CodeExpired,

        [EnumMember]
        PasswordRecovery_DatabaseError,

        [EnumMember]
        PasswordRecovery_ServerError,

        [EnumMember]
        PasswordRecovery_ConnectionError,

        [EnumMember]
        PasswordRecovery_UnexpectedError
    }
}
