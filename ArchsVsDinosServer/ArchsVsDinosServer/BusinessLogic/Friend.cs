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

                // Validar que no intente eliminarse a sí mismo
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
                        loggerHelper.LogWarning($"Inconsistencia en BD: se encontraron {friendships.Count} registros de amistad entre {username} y {friendUsername}. Se esperaban 2.");
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
            catch (DbEntityValidationException ex)
            {
                loggerHelper.LogError("Error de validacion en el metodo RemoveFriend", ex);
                return new FriendResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_DatabaseError
                };
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error inesperado en RemoveFriend: {ex.Message}", ex);
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
                loggerHelper.LogError($"Error de conexión a BD en GetFriends para usuario {username}", ex);
                return new FriendListResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_DatabaseError,
                    friends = new List<string>()
                };
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"Error SQL en GetFriends para usuario {username}", ex);
                return new FriendListResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_DatabaseError,
                    friends = new List<string>()
                };
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"Error de operación inválida en GetFriends para usuario {username}", ex);
                return new FriendListResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_UnexpectedError,
                    friends = new List<string>()
                };
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error inesperado en GetFriends para usuario {username}: {ex.Message}", ex);
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
                loggerHelper.LogError($"Error de conexión a BD en AreFriends", ex);
                return new FriendCheckResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_DatabaseError,
                    areFriends = false
                };
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"Error SQL en AreFriends", ex);
                return new FriendCheckResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_DatabaseError,
                    areFriends = false
                };
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"Error de operación inválida en AreFriends", ex);
                return new FriendCheckResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_UnexpectedError,
                    areFriends = false
                };
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error inesperado en AreFriends: {ex.Message}", ex);
                return new FriendCheckResponse
                {
                    success = false,
                    resultCode = FriendResultCode.Friend_UnexpectedError,
                    areFriends = false
                };
            }
        }

        private bool IsEmpty(string username, string friendUsername)
        {
            return validationHelper.IsEmpty(username) || validationHelper.IsEmpty(friendUsername);
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
