using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTO.Result_Codes
{
    [DataContract]
    public enum FriendRequestResultCode
    {
        [EnumMember] FriendRequest_Success,
        [EnumMember] FriendRequest_UserNotFound,
        [EnumMember] FriendRequest_AlreadyFriends,
        [EnumMember] FriendRequest_RequestAlreadySent,
        [EnumMember] FriendRequest_RequestNotFound,
        [EnumMember] FriendRequest_CannotSendToYourself,
        [EnumMember] FriendRequest_EmptyUsername,
        [EnumMember] FriendRequest_DatabaseError,
        [EnumMember] FriendRequest_UnexpectedError
    }
}
