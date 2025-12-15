using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Result_Codes
{
    [DataContract]
    public enum JoinMatchResultCode
    {
        [EnumMember] JoinMatch_Success,
        [EnumMember] JoinMatch_LobbyNotFound,
        [EnumMember] JoinMatch_LobbyFull,
        [EnumMember] JoinMatch_InvalidParameters,
        [EnumMember] JoinMatch_Timeout,
        [EnumMember] JoinMatch_InvalidSettings,
        [EnumMember] JoinMatch_UnexpectedError
    }
}
