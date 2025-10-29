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
                    loggerHelper.LogWarning("Intento de suscripción con username vacío o callback nulo");
                    return false;
                }

                lock (lockObject)
                {
                    if (!subscribers.ContainsKey(username))
                    {
                        subscribers[username] = callback;
                        loggerHelper.LogWarning($"Usuario {username} suscrito a notificaciones de solicitudes");
                        return true;
                    }
                    else
                    {
                        subscribers[username] = callback;
                        loggerHelper.LogWarning($"Usuario {username} actualizó su suscripción");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error al suscribir usuario {username}", ex);
                return false;
            }
        }

        public bool Unsubscribe(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return false;
                }

                lock (lockObject)
                {
                    if (subscribers.ContainsKey(username))
                    {
                        subscribers.Remove(username);
                        loggerHelper.LogWarning($"Usuario {username} desuscrito de notificaciones");
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error al desuscribir usuario {username}", ex);
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
                            loggerHelper.LogWarning($"Usuario {username} desconectado, removido de suscriptores");
                        }
                        catch (TimeoutException)
                        {
                            subscribers.Remove(username);
                            loggerHelper.LogWarning($"Timeout al notificar a {username}, removido de suscriptores");
                        }
                        catch (Exception ex)
                        {
                            loggerHelper.LogError($"Error al ejecutar callback para {username}", ex);
                            subscribers.Remove(username);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error al notificar al usuario {username}", ex);
            }
        }
    }
}
