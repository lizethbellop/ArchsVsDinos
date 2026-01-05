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
        Provoke_Success,
        [EnumMember]
        Provoke_NotYourTurn,
        [EnumMember]
        Provoke_InvalidArmyType,
        [EnumMember]
        Provoke_NoArchsInArmy,
        [EnumMember]
        Provoke_AlreadyTookAction,
        [EnumMember]
        Provoke_DatabaseError,
        [EnumMember]
        Provoke_UnexpectedError
    }
}
