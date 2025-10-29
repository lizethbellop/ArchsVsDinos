using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Result_Codes
{
    [DataContract]
    public enum FriendResultCode
    {
        [EnumMember] Friend_Success,
        [EnumMember] Friend_UserNotFound,
        [EnumMember] Friend_AlreadyFriends,
        [EnumMember] Friend_NotFriends,
        [EnumMember] Friend_CannotAddYourself,
        [EnumMember] Friend_DatabaseError,
        [EnumMember] Friend_UnexpectedError,
        [EnumMember] Friend_InternalError,
        [EnumMember] Friend_EmptyUsername,
        [EnumMember] Friend_NoFriends,
    }
}
