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
        GameSetup_Success,
        [EnumMember]
        GameSetup_MatchNotFound,
        [EnumMember]
        GameSetup_GameAlreadyInitialized,
        [EnumMember]
        GameSetup_NotEnoughPlayers,
        [EnumMember]
        GameSetup_DatabaseError,
        [EnumMember]
        GameSetup_UnexpectedError
    }
}
