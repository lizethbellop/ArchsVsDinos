using ArchsVsDinosClient.FriendService;
using ArchsVsDinosClient.Properties.Langs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Utils
{
    public static class FriendResultCodeHelper
    {
        public static string GetMessage(FriendResultCode resultCode)
        {
            switch (resultCode)
            {
                case FriendResultCode.Friend_Success:
                    return Lang.Friend_Success;
                case FriendResultCode.Friend_UserNotFound:
                    return Lang.Friend_UserNotFound;
                case FriendResultCode.Friend_AlreadyFriends:
                    return Lang.Friend_AlreadyFriends;
                case FriendResultCode.Friend_NotFriends:
                    return Lang.Friend_NotFriends;
                case FriendResultCode.Friend_CannotAddYourself:
                    return Lang.Friend_CannotAddYourself;
                case FriendResultCode.Friend_DatabaseError:
                    return Lang.Friend_DatabaseError;
                case FriendResultCode.Friend_UnexpectedError:
                    return Lang.Friend_UnexpectedError;
                default:
                    return Lang.Friend_UnexpectedError;
            }
        }
    }
}
