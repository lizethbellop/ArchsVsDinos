using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.Interfaces;
using Contracts;
using Contracts.DTO.Response;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    public class FriendManager : IFriendManager
    {

        private readonly Friend friendLogic;
        private readonly ILoggerHelper loggerHelper;

        public FriendManager()
        {
            loggerHelper = new Wrappers.LoggerHelperWrapper();
            friendLogic = new Friend();
        }

        public FriendManager(Friend logic, ILoggerHelper logger)
        {
            friendLogic = logic;
            loggerHelper = logger;
        }
        public FriendCheckResponse AreFriends(string username, string friendUsername)
        {
            try
            {
                return friendLogic.AreFriends(username, friendUsername);
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError($"Database connection error in AreFriends service for users {username} and {friendUsername}", ex);
                return new FriendCheckResponse
                {
                    Success = false,
                    ResultCode = FriendResultCode.Friend_DatabaseError,
                    AreFriends = false
                };
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"SQL error in AreFriends service for users {username} and {friendUsername}", ex);
                return new FriendCheckResponse
                {
                    Success = false,
                    ResultCode = FriendResultCode.Friend_DatabaseError,
                    AreFriends = false
                };
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Unexpected error in AreFriends service for users {username} and {friendUsername}", ex);
                return new FriendCheckResponse
                {
                    Success = false,
                    ResultCode = FriendResultCode.Friend_UnexpectedError,
                    AreFriends = false
                };
            }
        }

        public FriendListResponse GetFriends(string username)
        {
            try
            {
                return friendLogic.GetFriends(username);
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError($"Database connection error in GetFriends service for user {username}", ex);
                return new FriendListResponse
                {
                    Success = false,
                    ResultCode = FriendResultCode.Friend_DatabaseError,
                    Friends = new List<string>()
                };
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"SQL error in GetFriends service for user {username}", ex);
                return new FriendListResponse
                {
                    Success = false,
                    ResultCode = FriendResultCode.Friend_DatabaseError,
                    Friends = new List<string>()
                };
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Unexpected error in GetFriends service for user {username}", ex);
                return new FriendListResponse
                {
                    Success = false,
                    ResultCode = FriendResultCode.Friend_UnexpectedError,
                    Friends = new List<string>()
                };
            }
        }

        public FriendResponse RemoveFriend(string username, string friendUsername)
        {
            try
            {
                return friendLogic.RemoveFriend(username, friendUsername);
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError($"Database connection error in RemoveFriend service for users {username} and {friendUsername}", ex);
                return new FriendResponse
                {
                    Success = false,
                    ResultCode = FriendResultCode.Friend_DatabaseError
                };
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"SQL error in RemoveFriend service for users {username} and {friendUsername}", ex);
                return new FriendResponse
                {
                    Success = false,
                    ResultCode = FriendResultCode.Friend_DatabaseError
                };
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Unexpected error in RemoveFriend service for users {username} and {friendUsername}", ex);
                return new FriendResponse
                {
                    Success = false,
                    ResultCode = FriendResultCode.Friend_UnexpectedError
                };
            }
        }
    }
}
