using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Result_Codes
{
    [DataContract]
    public enum PlayCardResultCode
    {
        [EnumMember]
        Success,
        [EnumMember]
        NotYourTurn,
        [EnumMember]
        CardNotInHand,
        [EnumMember]
        InvalidCardType,
        [EnumMember]
        AlreadyPlayedTwoCards,
        [EnumMember]
        MustAttachToHead,
        [EnumMember]
        InvalidDinoHead,
        [EnumMember]
        ArmyTypeMismatch,
        [EnumMember]
        DatabaseError,
        [EnumMember]
        UnexpectedError
    }
}
