using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts.DTO.Response;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


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

        public FriendResponse RemoveFriend(string username, string friendUsername)
        {
            return ExecuteWithErrorHandling(
                () => RemoveFriendCore(username, friendUsername),
                "RemoveFriend",
                string.Format("users {0} and {1}", username, friendUsername)
            );
        }

        public FriendListResponse GetFriends(string username)
        {
            return ExecuteWithErrorHandling(
                () => GetFriendsCore(username),
                "GetFriends",
                string.Format("user {0}", username)
            );
        }

        public FriendCheckResponse AreFriends(string username, string friendUsername)
        {
            return ExecuteWithErrorHandling(
                () => AreFriendsCore(username, friendUsername),
                "AreFriends",
                string.Format("users {0} and {1}", username, friendUsername)
            );
        }

        private FriendResponse RemoveFriendCore(string username, string friendUsername)
        {
            FriendResponse response = new FriendResponse();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(friendUsername))
            {
                response.Success = false;
                response.ResultCode = FriendResultCode.Friend_UserNotFound;
                return response;
            }

            if (username == friendUsername)
            {
                response.Success = false;
                response.ResultCode = FriendResultCode.Friend_CannotAddYourself;
                return response;
            }

            using (var context = contextFactory())
            {
                UserAccount user = context.UserAccount
                    .FirstOrDefault(u => u.username == username);
                UserAccount friendUser = context.UserAccount
                    .FirstOrDefault(u => u.username == friendUsername);

                if (user == null || friendUser == null)
                {
                    response.Success = false;
                    response.ResultCode = FriendResultCode.Friend_UserNotFound;
                    return response;
                }

                var friendships = context.Friendship
                    .Where(f => (f.idUser == user.idUser && f.idUserFriend == friendUser.idUser) ||
                                (f.idUser == friendUser.idUser && f.idUserFriend == user.idUser))
                    .ToList();

                if (friendships.Count == 0)
                {
                    response.Success = false;
                    response.ResultCode = FriendResultCode.Friend_NotFriends;
                    return response;
                }

                if (friendships.Count != 2)
                {
                    loggerHelper.LogWarning(string.Format("Database inconsistency: found {0} friendship records between {1} and {2}. Expected 2.",
                        friendships.Count, username, friendUsername));
                }

                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        foreach (var friendship in friendships)
                        {
                            context.Friendship.Remove(friendship);
                        }
                        context.SaveChanges();
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }

                response.Success = true;
                response.ResultCode = FriendResultCode.Friend_Success;
                return response;
            }
        }

        private FriendListResponse GetFriendsCore(string username)
        {
            FriendListResponse response = new FriendListResponse();

            if (IsUsernameEmpty(username))
            {
                response.Success = false;
                response.ResultCode = FriendResultCode.Friend_EmptyUsername;
                response.Friends = new List<string>();
                return response;
            }

            using (var context = contextFactory())
            {
                UserAccount user = GetUserByUsername(context, username);

                if (user == null)
                {
                    response.Success = false;
                    response.ResultCode = FriendResultCode.Friend_UserNotFound;
                    response.Friends = new List<string>();
                    return response;
                }

                List<string> friends = GetFriendsList(context, user.idUser);

                response.Success = true;
                response.ResultCode = FriendResultCode.Friend_Success;
                response.Friends = friends;
                return response;
            }
        }

        private FriendCheckResponse AreFriendsCore(string username, string friendUsername)
        {
            FriendCheckResponse response = new FriendCheckResponse();

            if (IsUsernameEmpty(username) || IsUsernameEmpty(friendUsername))
            {
                response.Success = false;
                response.ResultCode = FriendResultCode.Friend_EmptyUsername;
                response.AreFriends = false;
                return response;
            }

            using (var context = contextFactory())
            {
                UserAccount user = GetUserByUsername(context, username);
                UserAccount friendUser = GetUserByUsername(context, friendUsername);

                if (user == null || friendUser == null)
                {
                    response.Success = false;
                    response.ResultCode = FriendResultCode.Friend_UserNotFound;
                    response.AreFriends = false;
                    return response;
                }

                bool areFriends = IsFriendshipExists(context, user.idUser, friendUser.idUser);

                response.Success = true;
                response.ResultCode = FriendResultCode.Friend_Success;
                response.AreFriends = areFriends;
                return response;
            }
        }

        private bool IsUsernameEmpty(string username)
        {
            return validationHelper.IsEmpty(username);
        }

        private UserAccount GetUserByUsername(IDbContext context, string username)
        {
            return context.UserAccount.FirstOrDefault(u => u.username == username);
        }

        private List<string> GetFriendsList(IDbContext context, int userId)
        {
            return context.Friendship
                .Where(f => f.idUser == userId)
                .Select(f => f.UserAccount1.username)
                .Where(u => !string.IsNullOrEmpty(u))
                .Distinct()
                .ToList();
        }

        private bool IsFriendshipExists(IDbContext context, int userId, int friendUserId)
        {
            return context.Friendship.Any(f => f.idUser == userId && f.idUserFriend == friendUserId);
        }

        private T ExecuteWithErrorHandling<T>(Func<T> operation, string operationName, string context) where T : new()
        {
            try
            {
                return operation();
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError(string.Format("Database connection error in {0} for {1}", operationName, context), ex);
                return CreateErrorResponse<T>(FriendResultCode.Friend_DatabaseError);
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError(string.Format("SQL error in {0} for {1}", operationName, context), ex);
                return CreateErrorResponse<T>(FriendResultCode.Friend_DatabaseError);
            }
            catch (DbEntityValidationException ex)
            {
                loggerHelper.LogError(string.Format("Validation error in {0} for {1}", operationName, context), ex);
                return CreateErrorResponse<T>(FriendResultCode.Friend_DatabaseError);
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError(string.Format("Invalid operation in {0} for {1}", operationName, context), ex);
                return CreateErrorResponse<T>(FriendResultCode.Friend_UnexpectedError);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError(string.Format("Unexpected error in {0} for {1}: {2}", operationName, context, ex.Message), ex);
                return CreateErrorResponse<T>(FriendResultCode.Friend_UnexpectedError);
            }
        }

        private T CreateErrorResponse<T>(FriendResultCode resultCode) where T : new()
        {
            var response = new T();

            if (response is FriendResponse friendResponse)
            {
                friendResponse.Success = false;
                friendResponse.ResultCode = resultCode;
            }
            else if (response is FriendListResponse listResponse)
            {
                listResponse.Success = false;
                listResponse.ResultCode = resultCode;
                listResponse.Friends = new List<string>();
            }
            else if (response is FriendCheckResponse checkResponse)
            {
                checkResponse.Success = false;
                checkResponse.ResultCode = resultCode;
                checkResponse.AreFriends = false;
            }

            return response;
        }
    }
}
