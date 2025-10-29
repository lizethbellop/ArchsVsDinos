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
            try
            {
                FriendResponse response = new FriendResponse();

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(friendUsername))
                {
                    response.success = false;
                    response.resultCode = FriendResultCode.Friend_UserNotFound;
                    return response;
                }

                if (username == friendUsername)
                {
                    response.success = false;
                    response.resultCode = FriendResultCode.Friend_CannotAddYourself;
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
                        response.success = false;
                        response.resultCode = FriendResultCode.Friend_UserNotFound;
                        return response;
                    }

                    var friendships = context.Friendship
                        .Where(f => (f.idUser == user.idUser && f.idUserFriend == friendUser.idUser) ||
                                    (f.idUser == friendUser.idUser && f.idUserFriend == user.idUser))
                        .ToList();

                    if (friendships.Count == 0)
                    {
                        response.success = false;
                        response.resultCode = FriendResultCode.Friend_NotFriends;
                        return response;
                    }

                    if (friendships.Count != 2)
                    {
                        loggerHelper.LogWarning($"Database inconsistency: found {friendships.Count} friendship records between {username} and {friendUsername}. Expected 2.");
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

                    response.success = true;
                    response.resultCode = FriendResultCode.Friend_Success;
                    return response;
                }
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError($"Database connection error in RemoveFriend for users {username} and {friendUsername}", ex);
                return new FriendResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_DatabaseError
                };
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"SQL error in RemoveFriend for users {username} and {friendUsername}", ex);
                return new FriendResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_DatabaseError
                };
            }
            catch (DbEntityValidationException ex)
            {
                loggerHelper.LogError($"Validation error in RemoveFriend for users {username} and {friendUsername}", ex);
                return new FriendResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_DatabaseError
                };
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"Invalid operation in RemoveFriend for users {username} and {friendUsername}", ex);
                return new FriendResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_UnexpectedError
                };
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Unexpected error in RemoveFriend for users {username} and {friendUsername}: {ex.Message}", ex);
                return new FriendResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_UnexpectedError
                };
            }
        }

        public FriendListResponse GetFriends(string username)
        {
            try
            {
                FriendListResponse response = new FriendListResponse();

                if (IsUsernameEmpty(username))
                {
                    response.success = false;
                    response.resultCode = FriendResultCode.Friend_EmptyUsername;
                    response.friends = new List<string>();
                    return response;
                }

                using (var context = contextFactory())
                {
                    UserAccount user = GetUserByUsername(context, username);

                    if (user == null)
                    {
                        response.success = false;
                        response.resultCode = FriendResultCode.Friend_UserNotFound;
                        response.friends = new List<string>();
                        return response;
                    }

                    List<string> friends = GetFriendsList(context, user.idUser);

                    response.success = true;
                    response.resultCode = FriendResultCode.Friend_Success;
                    response.friends = friends;
                    return response;
                }
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError($"Database connection error in GetFriends for user {username}", ex);
                return new FriendListResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_DatabaseError,
                    friends = new List<string>()
                };
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"SQL error in GetFriends for user {username}", ex);
                return new FriendListResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_DatabaseError,
                    friends = new List<string>()
                };
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"Invalid operation in GetFriends for user {username}", ex);
                return new FriendListResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_UnexpectedError,
                    friends = new List<string>()
                };
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Unexpected error in GetFriends for user {username}: {ex.Message}", ex);
                return new FriendListResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_UnexpectedError,
                    friends = new List<string>()
                };
            }
        }

        public FriendCheckResponse AreFriends(string username, string friendUsername)
        {
            try
            {
                FriendCheckResponse response = new FriendCheckResponse();

                if (IsUsernameEmpty(username) || IsUsernameEmpty(friendUsername))
                {
                    response.success = false;
                    response.resultCode = FriendResultCode.Friend_EmptyUsername;
                    response.areFriends = false;
                    return response;
                }

                using (var context = contextFactory())
                {
                    UserAccount user = GetUserByUsername(context, username);
                    UserAccount friendUser = GetUserByUsername(context, friendUsername);

                    if (user == null || friendUser == null)
                    {
                        response.success = false;
                        response.resultCode = FriendResultCode.Friend_UserNotFound;
                        response.areFriends = false;
                        return response;
                    }

                    bool areFriends = IsFriendshipExists(context, user.idUser, friendUser.idUser);

                    response.success = true;
                    response.resultCode = FriendResultCode.Friend_Success;
                    response.areFriends = areFriends;
                    return response;
                }
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError($"Database connection error in AreFriends for users {username} and {friendUsername}", ex);
                return new FriendCheckResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_DatabaseError,
                    areFriends = false
                };
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"SQL error in AreFriends for users {username} and {friendUsername}", ex);
                return new FriendCheckResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_DatabaseError,
                    areFriends = false
                };
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"Invalid operation in AreFriends for users {username} and {friendUsername}", ex);
                return new FriendCheckResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_UnexpectedError,
                    areFriends = false
                };
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Unexpected error in AreFriends for users {username} and {friendUsername}: {ex.Message}", ex);
                return new FriendCheckResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_UnexpectedError,
                    areFriends = false
                };
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
    }
}
