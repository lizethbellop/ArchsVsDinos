using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Result_Codes
{
    [DataContract]
    public enum ProvokeResultCode
    {
        [EnumMember]
        Success,
        [EnumMember]
        NotYourTurn,
        [EnumMember]
        InvalidArmyType,
        [EnumMember]
        NoArchsInArmy,
        [EnumMember]
        AlreadyTookAction,
        [EnumMember]
        DatabaseError,
        [EnumMember]
        UnexpectedError
    }
}
