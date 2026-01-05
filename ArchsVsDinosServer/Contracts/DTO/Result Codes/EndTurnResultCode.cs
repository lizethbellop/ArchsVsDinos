using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Result_Codes
{
    [DataContract]
    public enum EndTurnResultCode
    {
        [EnumMember]
        EndTurn_Success,
        [EnumMember]
        EndTurn_NotYourTurn,
        [EnumMember]
        EndTurn_GameEnded,
        [EnumMember]
        EndTurn_DatabaseError,
        [EnumMember]
        EndTurn_UnexpectedError
    }
}
