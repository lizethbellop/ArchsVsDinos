using ArchsVsDinosServer.Interfaces;
using Contracts;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic
{

    public class Chat
    {
        private static readonly ConcurrentDictionary<string, IChatManagerCallback> ConnectedUsers = new ConcurrentDictionary<string, IChatManagerCallback>();
        private const string DefaultRoom = "Lobby";
        private readonly ILoggerHelper loggerHelper;

        public Chat(ILoggerHelper loggerHelper)
        {
            this.loggerHelper = loggerHelper;
        }

        public ChatResultCode Connect(string username)
        {
            IChatManagerCallback callback;

            try
            {
                callback = OperationContext.Current.GetCallbackChannel<IChatManagerCallback>();
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"Error obtaining the callback: {ex.Message}", ex);
                return ChatResultCode.Chat_ConnectionError;
            }

            if (!ConnectedUsers.TryAdd(username, callback))
            {
                try
                {
                    callback.ReceiveSystemNotification(ChatResultCode.Chat_UserAlreadyConnected, "User already connected");
                }
                catch (CommunicationException)
                {
                    loggerHelper.LogWarning($"Communication error with user {username}");
                }
                catch (TimeoutException)
                {
                    loggerHelper.LogWarning($"Timeout communicating with user {username}");
                }
                return ChatResultCode.Chat_UserAlreadyConnected;
            }

            BroadcastSystemNotificationWithEnum(ChatResultCode.Chat_UserConnected, $"{username} has joined");
            UpdateUserList();
            return ChatResultCode.Chat_UserConnected;
        }

        public void Disconnect(string username)
        {
            if (ConnectedUsers.TryRemove(username, out _))
            {
                BroadcastSystemNotificationWithEnum(ChatResultCode.Chat_UserDisconnected, $"{username} has left the lobby");
                UpdateUserList();
            }
        }

        public void SendMessageToRoom(string message, string username)
        {
            // TODO: agregar filtro de palabras
            BroadcastMessageToAll(username, message);
        }

        private void BroadcastSystemNotificationWithEnum(ChatResultCode code, string message)
        {
            foreach (var user in ConnectedUsers.ToArray())
            {
                SafeCallbackInvoke(user.Key, () =>
                {
                    user.Value.ReceiveSystemNotification(code, message);
                });
            }
        }

        private void BroadcastMessageToAll(string fromUser, string message)
        {
            foreach (var user in ConnectedUsers.ToArray())
            {
                SafeCallbackInvoke(user.Key, () =>
                {
                    user.Value.ReceiveMessage(DefaultRoom, fromUser, message);
                });
            }
        }

        private void UpdateUserList()
        {
            var users = ConnectedUsers.Keys.ToList();

            foreach (var user in ConnectedUsers.ToArray())
            {
                SafeCallbackInvoke(user.Key, () =>
                {
                    user.Value.UpdateUserList(users);
                });
            }
        }

        private void SafeCallbackInvoke(string username, Action callbackAction)
        {
            try
            {
                callbackAction();
            }
            catch (CommunicationObjectAbortedException)
            {
                ConnectedUsers.TryRemove(username, out _);
            }
            catch (CommunicationException)
            {
                loggerHelper.LogWarning($"Communication error with user {username}");
                ConnectedUsers.TryRemove(username, out _);
            }
            catch (TimeoutException)
            {
                loggerHelper.LogWarning($"Timeout communicating with user {username}");
                ConnectedUsers.TryRemove(username, out _);
            }
            catch (ObjectDisposedException)
            {
                ConnectedUsers.TryRemove(username, out _);
            }
        }
    }
}
