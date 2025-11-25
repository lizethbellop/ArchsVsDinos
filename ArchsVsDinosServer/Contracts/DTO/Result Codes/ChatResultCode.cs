using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Result_Codes
{
    [DataContract]
    public enum ChatResultCode
    {
        [EnumMember] Chat_UserConnected,
        [EnumMember] Chat_UserDisconnected,
        [EnumMember] Chat_UserAlreadyConnected,
        [EnumMember] Chat_ConnectionError,
        [EnumMember] Chat_MessageBlocked,
        [EnumMember] Chat_UserBanned,
        [EnumMember] Chat_Error
    }
}
