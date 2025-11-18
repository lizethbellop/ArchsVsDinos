using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Result_Codes
{
    [DataContract]
    public enum SaveMatchResultCode
    {
        [EnumMember]
        Success,
        [EnumMember]
        MatchNotFound,
        [EnumMember]
        InvalidData,
        [EnumMember]
        DatabaseError,
        [EnumMember]
        UnexpectedError
    }
}
