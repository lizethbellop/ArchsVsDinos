using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Result_Codes
{
    [DataContract]
    public enum DrawCardResultCode
    {
        [EnumMember]
        DrawCard_Success,
        [EnumMember]
        DrawCard_NotYourTurn,
        [EnumMember]
        DrawCard_DrawPileEmpty,
        [EnumMember]
        DrawCard_InvalidDrawPile,
        [EnumMember]
        DrawCard_InvalidParameter,
        [EnumMember]
        DrawCard_NoCards,
        [EnumMember]
        DrawCard_AlreadyDrewThisTurn,
        [EnumMember]
        DrawCard_GameNotStarted,
        [EnumMember]
        DrawCard_DatabaseError,
        [EnumMember]
        DrawCard_UnexpectedError
    }
}
