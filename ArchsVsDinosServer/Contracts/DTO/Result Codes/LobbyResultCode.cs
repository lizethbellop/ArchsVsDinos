using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Result_Codes
{
    [DataContract]
    public enum LobbyResultCode
    {
        [EnumMember] Lobby_LobbyCreated,
        [EnumMember] Lobby_LobbyJoined,
        [EnumMember] Lobby_LobbyCreationError,
        [EnumMember] Lobby_LobbyJoinedError,
        [EnumMember] Lobby_ConnectionError,
        [EnumMember] Lobby_NotExist,
        [EnumMember] Lobby_NotHost,
        [EnumMember] Lobby_LobbyCancelled,
        [EnumMember] Lobby_LobbyCancelationError,
        [EnumMember] Lobby_LobbyLeft,
        [EnumMember] Lobby_LobbyLeftError,
        [EnumMember] Lobby_PlayerExpelled,
        [EnumMember] Lobby_PlayerExpelledError,
        [EnumMember] Lobby_FullLobby,
        [EnumMember] Lobby_GameStarted,
        [EnumMember] Lobby_IncompleteLobby,
        [EnumMember] Lobby_ErrorGame
    }

}
