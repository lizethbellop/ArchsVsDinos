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

        public ChatResultCode Connect(string username)
        {
            IChatManagerCallback callback;
            try
            {
                callback = OperationContext.Current.GetCallbackChannel<IChatManagerCallback>();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Error obtaining the callback: {ex.Message}");
                return ChatResultCode.Chat_ConnectionError;
            }

            if (ConnectedUsers.ContainsKey(username))
            {
                try
                {
                    callback.ReceiveSystemNotification(ChatResultCode.Chat_UserAlreadyConnected, "User already connected");
                }
                catch (CommunicationException ex)
                {
                    Console.WriteLine($"Communication error while notifying: {ex.Message}");
                }
                catch (TimeoutException ex)
                {
                    Console.WriteLine($"Timeout while notifying: {ex.Message}");
                }
                return ChatResultCode.Chat_UserAlreadyConnected;
            }

            ConnectedUsers[username] = callback;
            BroadcastSystemNotificationWithEnum(ChatResultCode.Chat_UserConnected, $"{username} has joined");
            UpdateUserList();
            return ChatResultCode.Chat_UserConnected;
        }

        public ChatResultCode Disconnect(string username)
        {
            if (ConnectedUsers.TryRemove(username, out _))
            {
                BroadcastSystemNotificationWithEnum(ChatResultCode.Chat_UserDisconnected, $"{username} has left the lobby");
                UpdateUserList();
                return ChatResultCode.Chat_UserDisconnected;
            }
            return ChatResultCode.Chat_ConnectionError;
        }

        public void SendMessageToRoom(string message, string username)
        {
            // TODO: agregar filtro de palabras
            BroadcastMessageToAll(username, message);
        }

        public List<string> GetConnectedUsers()
        {
            return ConnectedUsers.Keys.ToList();
        }

        private void BroadcastSystemNotificationWithEnum(ChatResultCode code, string message)
        {
            foreach (var user in ConnectedUsers.ToArray())
            {
                try
                {
                    user.Value.ReceiveSystemNotification(code, message);
                }
                catch (CommunicationObjectAbortedException)
                {
                    ConnectedUsers.TryRemove(user.Key, out _);
                }
                catch (CommunicationException)
                {
                    ConnectedUsers.TryRemove(user.Key, out _);
                }
                catch (TimeoutException)
                {
                    ConnectedUsers.TryRemove(user.Key, out _);
                }
                catch (ObjectDisposedException)
                {
                    ConnectedUsers.TryRemove(user.Key, out _);
                }
            }
        }

        private void BroadcastMessageToAll(string fromUser, string message)
        {
            foreach (var user in ConnectedUsers.ToArray())
            {
                try
                {
                    user.Value.ReceiveMessage("Lobby", fromUser, message);
                }
                catch (CommunicationObjectAbortedException)
                {
                    ConnectedUsers.TryRemove(user.Key, out _);
                }
                catch (CommunicationException)
                {
                    ConnectedUsers.TryRemove(user.Key, out _);
                }
                catch (TimeoutException)
                {
                    ConnectedUsers.TryRemove(user.Key, out _);
                }
                catch (ObjectDisposedException)
                {
                    ConnectedUsers.TryRemove(user.Key, out _);
                }
            }
        }

        private void UpdateUserList()
        {
            var users = ConnectedUsers.Keys.ToList();
            foreach (var user in ConnectedUsers.ToArray())
            {
                try
                {
                    user.Value.UpdateUserList(users);
                }
                catch (CommunicationObjectAbortedException)
                {
                    ConnectedUsers.TryRemove(user.Key, out _);
                }
                catch (CommunicationException)
                {
                    ConnectedUsers.TryRemove(user.Key, out _);
                }
                catch (TimeoutException)
                {
                    ConnectedUsers.TryRemove(user.Key, out _);
                }
                catch (ObjectDisposedException)
                {
                    ConnectedUsers.TryRemove(user.Key, out _);
                }
            }
        }
    }
}
