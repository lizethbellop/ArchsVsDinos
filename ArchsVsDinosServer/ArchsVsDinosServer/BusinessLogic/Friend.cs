using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts.DTO.Response;
using Contracts.DTO.Result_Codes;
using System.Data.Entity.Infrastructure;


namespace ArchsVsDinosServer.BusinessLogic
{
    public class Friend
    {
        private readonly ILoggerHelper loggerHelper;
        private readonly Func<IDbContext> contextFactory;
        private readonly IValidationHelper validationHelper;

        public Friend(ServiceDependencies dependencies)
        {
            loggerHelper = dependencies.loggerHelper;
            contextFactory = dependencies.contextFactory;
            validationHelper = dependencies.validationHelper;
        }

        public Friend() : this(new ServiceDependencies())
        {
        }

        public FriendResponse AddFriend(string username, string friendUsername)
        {
            try
            {
                FriendResponse response = new FriendResponse();

                if (IsEmpty(username, friendUsername))
                {
                    response.success = false;
                    response.resultCode = FriendResultCode.Friend_UserNotFound;
                    return response;
                }

                using (var context = contextFactory())
                {
                    UserAccount user = context.UserAccount
                        .FirstOrDefault(u => u.username == username);
                    UserAccount friendUser = context.UserAccount
                        .FirstOrDefault(u => u.username == friendUsername);

                    if (user == null | friendUser == null)
                    {
                        response.success = false;
                        response.resultCode = FriendResultCode.Friend_UserNotFound;
                        return response;
                    }

                    if (user.idUser == friendUser.idUser)
                    {
                        response.success = false;
                        response.resultCode = FriendResultCode.Friend_CannotAddYourself;
                        return response;
                    }



                    bool alreadyFriends = context.Friendship.Any(f => (f.idUser == user.idUser && f.idUserFriend == friendUser.idUser) || (f.idUser == friendUser.idUser && f.idUserFriend == user.idUser));

                    if (alreadyFriends)
                    {
                        response.success = false;
                        response.resultCode = FriendResultCode.Friend_AlreadyFriends;
                        return response;
                    }

                    Friendship friendship1 = new Friendship
                    {
                        idUser = user.idUser,
                        idUserFriend = friendUser.idUser,
                        status = "Pending"
                    };

                    Friendship friendship2 = new Friendship
                    {
                        idUser = friendUser.idUser,
                        idUserFriend = user.idUser,
                        status = "Pending"
                    };

                    context.Friendship.Add(friendship1);
                    context.Friendship.Add(friendship2);
                    context.SaveChanges();

                    response.success = true;
                    response.resultCode = FriendResultCode.Friend_Success;
                    return response;
                }
            }
            catch (DbUpdateException ex)
            {
                loggerHelper.LogError($"Database error in AddFriend: {ex.Message}", ex);
                return new FriendResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_DatabaseError,
                };
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"Database error in AddFriend: {ex.Message}", ex);
                return new FriendResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_InternalError
                };
            }
        }

        private bool IsEmpty(string username, string friendUsername)
        {
            return validationHelper.IsEmpty(username) || validationHelper.IsEmpty(friendUsername);
        }
    }
}
