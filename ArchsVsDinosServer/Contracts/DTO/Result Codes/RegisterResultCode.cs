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
        [EnumMember] Success,
        [EnumMember] UsernameExists,
        [EnumMember] NicknameExists,
        [EnumMember] BothExists,
        [EnumMember] DatabaseError
    }
}
