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
        Success,
        [EnumMember]
        NotYourTurn,
        [EnumMember]
        DrawPileEmpty,
        [EnumMember]
        InvalidDrawPile,
        [EnumMember]
        AlreadyDrewThisTurn,
        [EnumMember]
        GameNotStarted,
        [EnumMember]
        DatabaseError,
        [EnumMember]
        UnexpectedError
    }
}
