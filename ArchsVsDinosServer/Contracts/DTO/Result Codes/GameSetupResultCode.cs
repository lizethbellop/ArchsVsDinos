using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Result_Codes
{
    [DataContract]
    public enum GameSetupResultCode
    {
        [EnumMember]
        Success,
        [EnumMember]
        MatchNotFound,
        [EnumMember]
        GameAlreadyInitialized,
        [EnumMember]
        NotEnoughPlayers,
        [EnumMember]
        DatabaseError,
        [EnumMember]
        UnexpectedError
    }
}
