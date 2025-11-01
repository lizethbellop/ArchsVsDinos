using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts.DTO.Response;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataModel = ArchsVsDinosServer;

namespace ArchsVsDinosServer.BusinessLogic
{
    public class FriendRequestLogic
    {
        private readonly ILoggerHelper loggerHelper;
        private readonly Func<IDbContext> contextFactory;
        private readonly IValidationHelper validationHelper;

        public FriendRequestLogic(ServiceDependencies dependencies)
        {
            loggerHelper = dependencies.loggerHelper;
            contextFactory = dependencies.contextFactory;
            validationHelper = dependencies.validationHelper;
        }

        public FriendRequestLogic() : this(new ServiceDependencies())
        {
        }

        public FriendRequestResponse SendFriendRequest(string fromUser, string toUser)
        {
            try
            {
                FriendRequestResponse response = new FriendRequestResponse();

                if (IsEmpty(fromUser, toUser))
                {
                    response.Success = false;
                    response.ResultCode = FriendRequestResultCode.FriendRequest_EmptyUsername;
                    return response;
                }

                using (var context = contextFactory())
                {
                    UserAccount sender = GetUserByUsername(context, fromUser);
                    UserAccount receiver = GetUserByUsername(context, toUser);

                    if (sender == null || receiver == null)
                    {
                        response.Success = false;
                        response.ResultCode = FriendRequestResultCode.FriendRequest_UserNotFound;
                        return response;
                    }

                    if (sender.idUser == receiver.idUser)
                    {
                        response.Success = false;
                        response.ResultCode = FriendRequestResultCode.FriendRequest_CannotSendToYourself;
                        return response;
                    }

                    bool alreadyFriends = IsFriendshipExists(context, sender.idUser, receiver.idUser);

                    if (alreadyFriends)
                    {
                        response.Success = false;
                        response.ResultCode = FriendRequestResultCode.FriendRequest_AlreadyFriends;
                        return response;
                    }

                    bool existingRequest = IsPendingRequestExists(context, sender.idUser, receiver.idUser);

                    if (existingRequest)
                    {
                        response.Success = false;
                        response.ResultCode = FriendRequestResultCode.FriendRequest_RequestAlreadySent;
                        return response;
                    }

                    FriendRequest friendRequest = context.FriendRequest.Create();
                    friendRequest.idUser = sender.idUser;
                    friendRequest.idReceiverUser = receiver.idUser;
                    friendRequest.date = DateTime.Now;
                    friendRequest.status = "Pending";

                    context.FriendRequest.Add(friendRequest);
                    context.SaveChanges();

                    response.Success = true;
                    response.ResultCode = FriendRequestResultCode.FriendRequest_Success;
                    return response;
                }
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError($"Error de conexión a BD en SendFriendRequest", ex);
                return new FriendRequestResponse
                {
                    Success = false,
                    ResultCode = FriendRequestResultCode.FriendRequest_DatabaseError
                };
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"Error SQL en SendFriendRequest", ex);
                return new FriendRequestResponse
                {
                    Success = false,
                    ResultCode = FriendRequestResultCode.FriendRequest_DatabaseError
                };
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"Error de operación inválida en SendFriendRequest", ex);
                return new FriendRequestResponse
                {
                    Success = false,
                    ResultCode = FriendRequestResultCode.FriendRequest_UnexpectedError
                };
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error inesperado en SendFriendRequest: {ex.Message}", ex);
                return new FriendRequestResponse
                {
                    Success = false,
                    ResultCode = FriendRequestResultCode.FriendRequest_UnexpectedError
                };
            }
        }

        public FriendRequestResponse AcceptFriendRequest(string fromUser, string toUser)
        {
            try
            {
                FriendRequestResponse response = new FriendRequestResponse();

                if (IsEmpty(fromUser, toUser))
                {
                    response.Success = false;
                    response.ResultCode = FriendRequestResultCode.FriendRequest_EmptyUsername;
                    return response;
                }

                using (var context = contextFactory())
                {
                    UserAccount sender = GetUserByUsername(context, fromUser);
                    UserAccount receiver = GetUserByUsername(context, toUser);

                    if (sender == null || receiver == null)
                    {
                        response.Success = false;
                        response.ResultCode = FriendRequestResultCode.FriendRequest_UserNotFound;
                        return response;
                    }

                    FriendRequest friendRequest = context.FriendRequest
                        .FirstOrDefault(fr => fr.idUser == sender.idUser &&
                                             fr.idReceiverUser == receiver.idUser &&
                                             fr.status == "Pending");

                    if (friendRequest == null)
                    {
                        response.Success = false;
                        response.ResultCode = FriendRequestResultCode.FriendRequest_RequestNotFound;
                        return response;
                    }

                    using (var transaction = context.Database.BeginTransaction())
                    {
                        try
                        {
                            friendRequest.status = "Accepted";

                            Friendship friendship1 = context.Friendship.Create();
                            friendship1.idUser = sender.idUser;
                            friendship1.idUserFriend = receiver.idUser;
                            friendship1.status = "Active";

                            Friendship friendship2 = context.Friendship.Create();
                            friendship2.idUser = receiver.idUser;
                            friendship2.idUserFriend = sender.idUser;
                            friendship2.status = "Active";

                            context.Friendship.Add(friendship1);
                            context.Friendship.Add(friendship2);
                            context.SaveChanges();
                            transaction.Commit();

                            response.Success = true;
                            response.ResultCode = FriendRequestResultCode.FriendRequest_Success;
                            return response;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError($"Error de conexión a BD en AcceptFriendRequest", ex);
                return new FriendRequestResponse
                {
                    Success = false,
                    ResultCode = FriendRequestResultCode.FriendRequest_DatabaseError
                };
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"Error SQL en AcceptFriendRequest", ex);
                return new FriendRequestResponse
                {
                    Success = false,
                    ResultCode = FriendRequestResultCode.FriendRequest_DatabaseError
                };
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"Error de operación inválida en AcceptFriendRequest", ex);
                return new FriendRequestResponse
                {
                    Success = false,
                    ResultCode = FriendRequestResultCode.FriendRequest_UnexpectedError
                };
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error inesperado en AcceptFriendRequest: {ex.Message}", ex);
                return new FriendRequestResponse
                {
                    Success = false,
                    ResultCode = FriendRequestResultCode.FriendRequest_UnexpectedError
                };
            }
        }

        public FriendRequestResponse RejectFriendRequest(string fromUser, string toUser)
        {
            try
            {
                FriendRequestResponse response = new FriendRequestResponse();

                if (IsEmpty(fromUser, toUser))
                {
                    response.Success = false;
                    response.ResultCode = FriendRequestResultCode.FriendRequest_EmptyUsername;
                    return response;
                }

                using (var context = contextFactory())
                {
                    UserAccount sender = GetUserByUsername(context, fromUser);
                    UserAccount receiver = GetUserByUsername(context, toUser);

                    if (sender == null || receiver == null)
                    {
                        response.Success = false;
                        response.ResultCode = FriendRequestResultCode.FriendRequest_UserNotFound;
                        return response;
                    }

                    var friendRequest = context.FriendRequest
                        .FirstOrDefault(fr => fr.idUser == sender.idUser &&
                                             fr.idReceiverUser == receiver.idUser &&
                                             fr.status == "Pending");

                    if (friendRequest == null)
                    {
                        response.Success = false;
                        response.ResultCode = FriendRequestResultCode.FriendRequest_RequestNotFound;
                        return response;
                    }

                    friendRequest.status = "Rejected";
                    context.SaveChanges();

                    response.Success = true;
                    response.ResultCode = FriendRequestResultCode.FriendRequest_Success;
                    return response;
                }
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError($"Error de conexión a BD en RejectFriendRequest", ex);
                return new FriendRequestResponse
                {
                    Success = false,
                    ResultCode = FriendRequestResultCode.FriendRequest_DatabaseError
                };
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"Error SQL en RejectFriendRequest", ex);
                return new FriendRequestResponse
                {
                    Success = false,
                    ResultCode = FriendRequestResultCode.FriendRequest_DatabaseError
                };
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"Error de operación inválida en RejectFriendRequest", ex);
                return new FriendRequestResponse
                {
                    Success = false,
                    ResultCode = FriendRequestResultCode.FriendRequest_UnexpectedError
                };
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error inesperado en RejectFriendRequest: {ex.Message}", ex);
                return new FriendRequestResponse
                {
                    Success = false,
                    ResultCode = FriendRequestResultCode.FriendRequest_UnexpectedError
                };
            }
        }

        public FriendRequestListResponse GetPendingRequests(string username)
        {
            try
            {
                FriendRequestListResponse response = new FriendRequestListResponse();

                if (IsUsernameEmpty(username))
                {
                    response.Success = false;
                    response.ResultCode = FriendRequestResultCode.FriendRequest_EmptyUsername;
                    response.Requests = new List<string>();
                    return response;
                }

                using (var context = contextFactory())
                {
                    UserAccount user = GetUserByUsername(context, username);

                    if (user == null)
                    {
                        response.Success = false;
                        response.ResultCode = FriendRequestResultCode.FriendRequest_UserNotFound;
                        response.Requests = new List<string>();
                        return response;
                    }

                    List<string> pendingRequests = GetPendingRequestsList(context, user.idUser);

                    response.Success = true;
                    response.ResultCode = FriendRequestResultCode.FriendRequest_Success;
                    response.Requests = pendingRequests;
                    return response;
                }
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError($"Error de conexión a BD en GetPendingRequests para usuario {username}", ex);
                return new FriendRequestListResponse
                {
                    Success = false,
                    ResultCode = FriendRequestResultCode.FriendRequest_DatabaseError,
                    Requests = new List<string>()
                };
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"Error SQL en GetPendingRequests para usuario {username}", ex);
                return new FriendRequestListResponse
                {
                    Success = false,
                    ResultCode = FriendRequestResultCode.FriendRequest_DatabaseError,
                    Requests = new List<string>()
                };
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"Error de operación inválida en GetPendingRequests para usuario {username}", ex);
                return new FriendRequestListResponse
                {
                    Success = false,
                    ResultCode = FriendRequestResultCode.FriendRequest_UnexpectedError,
                    Requests = new List<string>()
                };
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error inesperado en GetPendingRequests para usuario {username}: {ex.Message}", ex);
                return new FriendRequestListResponse
                {
                    Success = false,
                    ResultCode = FriendRequestResultCode.FriendRequest_UnexpectedError,
                    Requests = new List<string>()
                };
            }
        }

        private bool IsEmpty(string fromUser, string toUser)
        {
            return validationHelper.IsEmpty(fromUser) || validationHelper.IsEmpty(toUser);
        }

        private bool IsUsernameEmpty(string username)
        {
            return validationHelper.IsEmpty(username);
        }

        private UserAccount GetUserByUsername(IDbContext context, string username)
        {
            return context.UserAccount.FirstOrDefault(u => u.username == username);
        }

        private bool IsFriendshipExists(IDbContext context, int userId, int friendUserId)
        {
            return context.Friendship.Any(f => f.idUser == userId && f.idUserFriend == friendUserId);
        }

        private bool IsPendingRequestExists(IDbContext context, int senderId, int receiverId)
        {
            return context.FriendRequest.Any(fr => fr.idUser == senderId &&
                                                    fr.idReceiverUser == receiverId &&
                                                    fr.status == "Pending");
        }
        

        private List<string> GetPendingRequestsList(IDbContext context, int userId)
        {
            return context.FriendRequest
                .Where(fr => fr.idReceiverUser == userId && fr.status == "Pending")
                .Join(context.UserAccount,
                    fr => fr.idUser,
                    u => u.idUser,
                    (fr, u) => u.username)
                .Where(u => !string.IsNullOrEmpty(u))
                .Distinct()
                .ToList();
        }


    }
}
