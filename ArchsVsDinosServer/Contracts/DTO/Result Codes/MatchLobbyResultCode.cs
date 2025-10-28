using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Result_Codes
{
    [DataContract]
    public enum MatchLobbyResultCode
    {
        [EnumMember] Lobby_MatchLobbyCreated,
        [EnumMember] Lobby_MatchLobbyJoined,
        [EnumMember] Lobby_MatchLobbyCreationError,
        [EnumMember] Lobby_MatchLobbyJoinedError,
        [EnumMember] Lobby_ConnectionError
    }

}
