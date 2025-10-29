using ArchsVsDinosServer.BusinessLogic;
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

        // Constructor para pruebas unitarias
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

                // Notificar al que aceptó (toUser)
                callbackManager.NotifyFriendRequestAccepted(toUser, response.Success);

                // Si fue exitoso, notificar al que envió la solicitud (fromUser)
                if (response.Success)
                {
                    callbackManager.NotifyFriendRequestAccepted(fromUser, true);
                }
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error en AcceptFriendRequest del servicio", ex);
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
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error en GetPendingRequests del servicio", ex);
                callbackManager.NotifyPendingRequestsReceived(username, new List<string>());
            }
        }

        public void RejectFriendRequest(string fromUser, string toUser)
        {
            try
            {
                var response = friendRequestLogic.RejectFriendRequest(fromUser, toUser);

                // Notificar al que rechazó (toUser)
                callbackManager.NotifyFriendRequestRejected(toUser, response.Success);

                // Si fue exitoso, notificar al que envió la solicitud (fromUser)
                if (response.Success)
                {
                    callbackManager.NotifyFriendRequestRejected(fromUser, true);
                }
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error en RejectFriendRequest del servicio", ex);
                callbackManager.NotifyFriendRequestRejected(toUser, false);
            }
        }

        public void SendFriendRequest(string fromUser, string toUser)
        {
            try
            {
                var response = friendRequestLogic.SendFriendRequest(fromUser, toUser);

                // Notificar al remitente sobre el resultado
                callbackManager.NotifyFriendRequestSent(fromUser, response.Success);

                // Si fue exitoso, notificar al receptor
                if (response.Success)
                {
                    callbackManager.NotifyFriendRequestReceived(toUser, fromUser);
                }
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error en SendFriendRequest del servicio", ex);
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
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error en Subscribe del servicio para {username}", ex);
            }
        }

        public void Unsubscribe(string username)
        {
            try
            {
                callbackManager.Unsubscribe(username);
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Error en Unsubscribe del servicio para {username}", ex);
            }
        }
    }
}
