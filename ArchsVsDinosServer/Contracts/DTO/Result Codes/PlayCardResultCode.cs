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
        PlayCard_Success,
        [EnumMember]
        PlayCard_NotYourTurn,
        [EnumMember]
        PlayCard_CardNotInHand,
        [EnumMember]
        PlayCard_InvalidCardType,
        [EnumMember]
        PlayCard_AlreadyPlayedTwoCards,
        [EnumMember]
        PlayCard_MustAttachToHead,
        [EnumMember]
        PlayCard_InvalidDinoHead,
        [EnumMember]
        PlayCard_ArmyTypeMismatch,
        [EnumMember]
        PlayCard_DatabaseError,
        [EnumMember]
        PlayCard_UnexpectedError
    }
}
