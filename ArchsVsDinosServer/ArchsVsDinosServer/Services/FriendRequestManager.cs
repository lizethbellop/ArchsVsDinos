using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.Interfaces;
using Contracts;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class FriendRequestManager : IFriendRequestManager
    {

        private readonly FriendRequestLogic friendRequestLogic;
        private readonly FriendRequestCallbackManager callbackManager;
        private readonly ILoggerHelper loggerHelper;

        public FriendRequestManager()
        {
            loggerHelper = new Wrappers.LoggerHelperWrapper();
            friendRequestLogic = new FriendRequestLogic();
            callbackManager = new FriendRequestCallbackManager(loggerHelper);
        }

        public FriendRequestManager(FriendRequestLogic logic, FriendRequestCallbackManager manager, ILoggerHelper logger)
        {
            friendRequestLogic = logic;
            callbackManager = manager;
            loggerHelper = logger;
        }
        public void AcceptFriendRequest(string fromUser, string toUser)
        {
            try
            {
                var response = friendRequestLogic.AcceptFriendRequest(fromUser, toUser);
                callbackManager.NotifyFriendRequestAccepted(toUser, response.Success);

                if (response.Success)
                {
                    callbackManager.NotifyFriendRequestAccepted(fromUser, true);
                }
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError($"Database connection error in AcceptFriendRequest service for users {fromUser} and {toUser}", ex);
                callbackManager.NotifyFriendRequestAccepted(toUser, false);
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"SQL error in AcceptFriendRequest service for users {fromUser} and {toUser}", ex);
                callbackManager.NotifyFriendRequestAccepted(toUser, false);
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError($"Communication error while notifying AcceptFriendRequest for users {fromUser} and {toUser}", ex);
                callbackManager.NotifyFriendRequestAccepted(toUser, false);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Unexpected error in AcceptFriendRequest service for users {fromUser} and {toUser}", ex);
                callbackManager.NotifyFriendRequestAccepted(toUser, false);
            }
        }

        public void GetPendingRequests(string username)
        {
            try
            {
                var response = friendRequestLogic.GetPendingRequests(username);

                if (response.Success)
                {
                    callbackManager.NotifyPendingRequestsReceived(username, response.Requests);
                }
                else
                {
                    callbackManager.NotifyPendingRequestsReceived(username, new List<string>());
                }
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError($"Database connection error in GetPendingRequests service for user {username}", ex);
                callbackManager.NotifyPendingRequestsReceived(username, new List<string>());
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"SQL error in GetPendingRequests service for user {username}", ex);
                callbackManager.NotifyPendingRequestsReceived(username, new List<string>());
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError($"Communication error while notifying GetPendingRequests for user {username}", ex);
                callbackManager.NotifyPendingRequestsReceived(username, new List<string>());
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Unexpected error in GetPendingRequests service for user {username}", ex);
                callbackManager.NotifyPendingRequestsReceived(username, new List<string>());
            }
        }

        public void RejectFriendRequest(string fromUser, string toUser)
        {
            try
            {
                var response = friendRequestLogic.RejectFriendRequest(fromUser, toUser);
                callbackManager.NotifyFriendRequestRejected(toUser, response.Success);

                if (response.Success)
                {
                    callbackManager.NotifyFriendRequestRejected(fromUser, true);
                }
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError($"Database connection error in RejectFriendRequest service for users {fromUser} and {toUser}", ex);
                callbackManager.NotifyFriendRequestRejected(toUser, false);
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"SQL error in RejectFriendRequest service for users {fromUser} and {toUser}", ex);
                callbackManager.NotifyFriendRequestRejected(toUser, false);
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError($"Communication error while notifying RejectFriendRequest for users {fromUser} and {toUser}", ex);
                callbackManager.NotifyFriendRequestRejected(toUser, false);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Unexpected error in RejectFriendRequest service for users {fromUser} and {toUser}", ex);
                callbackManager.NotifyFriendRequestRejected(toUser, false);
            }
        }

        public void SendFriendRequest(string fromUser, string toUser)
        {
            try
            {
                var response = friendRequestLogic.SendFriendRequest(fromUser, toUser);
                callbackManager.NotifyFriendRequestSent(fromUser, response.Success);

                if (response.Success)
                {
                    callbackManager.NotifyFriendRequestReceived(toUser, fromUser);
                }
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError($"Database connection error in SendFriendRequest service for users {fromUser} and {toUser}", ex);
                callbackManager.NotifyFriendRequestSent(fromUser, false);
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"SQL error in SendFriendRequest service for users {fromUser} and {toUser}", ex);
                callbackManager.NotifyFriendRequestSent(fromUser, false);
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError($"Communication error while notifying SendFriendRequest for users {fromUser} and {toUser}", ex);
                callbackManager.NotifyFriendRequestSent(fromUser, false);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Unexpected error in SendFriendRequest service for users {fromUser} and {toUser}", ex);
                callbackManager.NotifyFriendRequestSent(fromUser, false);
            }
        }

        public void Subscribe(string username)
        {
            try
            {
                var callback = OperationContext.Current.GetCallbackChannel<IFriendRequestCallback>();
                callbackManager.Subscribe(username, callback);
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError($"Communication error in Subscribe service for user {username}", ex);
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"Invalid operation in Subscribe service for user {username}", ex);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Unexpected error in Subscribe service for user {username}", ex);
            }
        }

        public void Unsubscribe(string username)
        {
            try
            {
                callbackManager.Unsubscribe(username);
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"Invalid operation in Unsubscribe service for user {username}", ex);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Unexpected error in Unsubscribe service for user {username}", ex);
            }
        }
    }
}
