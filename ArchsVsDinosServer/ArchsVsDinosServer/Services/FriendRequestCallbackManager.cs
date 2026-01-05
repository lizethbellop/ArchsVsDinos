using ArchsVsDinosServer.Interfaces;
using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    public class FriendRequestCallbackManager
    {
        private readonly Dictionary<string, IFriendRequestCallback> subscribers;
        private readonly object lockObject;
        private readonly ILoggerHelper loggerHelper;

        public FriendRequestCallbackManager(ILoggerHelper loggerHelper)
        {
            subscribers = new Dictionary<string, IFriendRequestCallback>();
            lockObject = new object();
            this.loggerHelper = loggerHelper;
        }

        public bool Subscribe(string username, IFriendRequestCallback callback)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || callback == null)
                {
                    loggerHelper.LogWarning("Attempt to subscribe without username or callback");
                    return false;
                }

                lock (lockObject)
                {
                    if (!subscribers.ContainsKey(username))
                    {
                        subscribers[username] = callback;
                        loggerHelper.LogInfo($"User {username} subscribed to friend request notifications"); 
                        return true;
                    }
                    else
                    {
                        subscribers[username] = callback;
                        loggerHelper.LogInfo($"User {username} updated subscription");
                        return true;
                    }
                }
            }
            catch (CommunicationException ex)
            {
                loggerHelper.LogError($"Communication error while subscribing user {username}", ex);
                return false;
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"Invalid operation while subscribing user {username}", ex);
                return false;
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Unexpected error while subscribing user {username}", ex);
                return false;
            }
        }

        public bool Unsubscribe(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    loggerHelper.LogWarning("Attempt to unsubscribe without username");
                    return false;
                }

                lock (lockObject)
                {
                    if (subscribers.ContainsKey(username))
                    {
                        subscribers.Remove(username);
                        loggerHelper.LogInfo($"User {username} unsubscribed from notifications");
                        return true;
                    }

                    loggerHelper.LogWarning($"User {username} was not subscribed");
                    return false;
                }
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"Invalid operation while unsubscribing user {username}", ex);
                return false;
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Unexpected error while unsubscribing user {username}", ex);
                return false;
            }
        }

        public void NotifyFriendRequestSent(string username, bool success)
        {
            NotifyUser(username, callback => callback.OnFriendRequestSent(success));
        }

        public void NotifyFriendRequestReceived(string username, string fromUser)
        {
            NotifyUser(username, callback => callback.OnFriendRequestReceived(fromUser));
        }

        public void NotifyFriendRequestAccepted(string username, bool success)
        {
            NotifyUser(username, callback => callback.OnFriendRequestAccepted(success));
        }

        public void NotifyFriendRequestRejected(string username, bool success)
        {
            NotifyUser(username, callback => callback.OnFriendRequestRejected(success));
        }

        public void NotifyPendingRequestsReceived(string username, List<string> requests)
        {
            NotifyUser(username, callback => callback.OnPendingRequestsReceived(requests));
        }

        public bool IsUserSubscribed(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            lock (lockObject)
            {
                return subscribers.ContainsKey(username);
            }
        }

        public int GetSubscribersCount()
        {
            lock (lockObject)
            {
                return subscribers.Count;
            }
        }

        private void NotifyUser(string username, Action<IFriendRequestCallback> action)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return;
            }

            try
            {
                IFriendRequestCallback callback;

                lock (lockObject)
                {
                    if (subscribers.TryGetValue(username, out callback))
                    {
                        try
                        {
                            action(callback);
                        }
                        catch (CommunicationException)
                        {
                            subscribers.Remove(username);
                            loggerHelper.LogWarning($"User {username} disconnected, removed from subscribers");
                        }
                        catch (TimeoutException)
                        {
                            subscribers.Remove(username);
                            loggerHelper.LogWarning($"Timeout when notifying {username}, removed from subscribers");
                        }
                        catch (Exception ex)
                        {
                            loggerHelper.LogError($"Error executing callback for {username}", ex);
                            subscribers.Remove(username);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error notifying the user {username}", ex);
            }
        }
    }
}
