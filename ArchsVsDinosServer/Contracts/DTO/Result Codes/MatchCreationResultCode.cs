using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Result_Codes
{
    [DataContract]
    public enum MatchCreationResultCode
    {
        [EnumMember] MatchCreation_Success,
        [EnumMember] MatchCreation_Failure,
        [EnumMember] MatchCreation_InvalidParameters,
        [EnumMember] MatchCreation_ServerBusy,
        [EnumMember] MatchCreation_Timeout,
        [EnumMember] MatchCreation_InvalidSettings,
        [EnumMember] MatchCreation_UnexpectedError
    }
}
