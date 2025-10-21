using Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class Chat : IChatManager
    {
        private static readonly ConcurrentDictionary<string, IChatManagerCallback> ConnectedUsers = new ConcurrentDictionary<string, IChatManagerCallback>();
        
        public void Connect(string username)
        {

            IChatManagerCallback callback = null;
            try
            {
                callback = OperationContext.Current.GetCallbackChannel<IChatManagerCallback>();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Error obtaining the callback: {ex.Message}");
                throw new FaultException("A communication channel could not be estalished");
            }

            if (ConnectedUsers.ContainsKey(username))
            {
                try
                {
                    callback.ReceiveSystemNotification("Usuario ya conectado");
                }
                catch (CommunicationException ex)
                {
                    Console.WriteLine($"Communication error while notifying: {ex.Message}");
                }
                catch (TimeoutException ex)
                {
                    Console.WriteLine($"Timeout while notyfing: {ex.Message}");

                }
                return;
            }

            ConnectedUsers[username] = callback;
            Console.WriteLine($"{username} se ha unido al lobby");

            BroadcastSystemNotification($"{username} se ha unido al lobby");
        }

        public void Disconnect(string username)
        {
            if(ConnectedUsers.TryRemove(username, out _))
            {
                Console.WriteLine($"{username} se ha desconectado del lobby");
                BroadcastSystemNotification($"{username} ha salido del lobby");
            }
        }

        public void SendMessageToRoom(string message, string username)
        {
            Console.WriteLine($"[Lobby] {username}: {message}");
            BroadcastMessageToAll(username, message); ;
        }

        public void SendMessageToUser(string username, string targetUser, string message)
        {
            if (!ConnectedUsers.TryGetValue(targetUser, out var targetCallback))
            {
                if(ConnectedUsers.TryGetValue(username, out var senderCallback))
                {
                    NotifySender(username, $"El usuario '{targetUser}' no está conectado");
                }
                return;
            }

            try
            {
                targetCallback.ReceiveMessage("PRIVATE", username, message);
            }
            catch (CommunicationObjectAbortedException ex)
            {
                Console.WriteLine($"Aborted connection with {targetUser}: {ex.Message}");
                ConnectedUsers.TryRemove(targetUser, out _);
                NotifySender(username, $"El usuario '{targetUser}' se desconectó inesperadamente");
            }
            catch (CommunicationException ex)
            {
                Console.WriteLine($"Communication error {targetUser}: {ex.Message}");
                ConnectedUsers.TryRemove(targetUser, out _);
                NotifySender(username, $"No se pudo enviar el mensaje a '{targetUser}'");
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine($"Timeout while sending the message {targetUser}: {ex.Message}");
                NotifySender(username, $"Timeout al enviar mensaje a '{targetUser}'");
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"Channel closed for {targetUser}: {ex.Message}");
                ConnectedUsers.TryRemove(targetUser, out _);
            }
        }

        private void BroadcastSystemNotification(string notification)
        {
            foreach (var user in ConnectedUsers.ToArray())
            {
                try
                {
                    user.Value.ReceiveSystemNotification(notification);
                }
                catch (CommunicationObjectAbortedException ex)
                {
                    Console.WriteLine($"Connection aborted with {user.Key}: {ex.Message}");
                    ConnectedUsers.TryRemove(user.Key, out _);
                }
                catch (CommunicationException ex)
                {
                    Console.WriteLine($"Communication Error with {user.Key}: {ex.Message}");
                    ConnectedUsers.TryRemove(user.Key, out _);
                }
                catch (TimeoutException ex)
                {
                    Console.WriteLine($"Timeout to send notification {user.Key}: {ex.Message}");
                    ConnectedUsers.TryRemove(user.Key, out _);
                }
                catch (ObjectDisposedException ex)
                {
                    Console.WriteLine($"Channel closed for {user.Key}: {ex.Message}");
                    ConnectedUsers.TryRemove(user.Key, out _);
                }
            }
        }

        private void BroadcastMessageToAll(string fromUser, string message)
        {
            List<KeyValuePair<string, IChatManagerCallback>> usersList = new List<KeyValuePair<string, IChatManagerCallback>>(ConnectedUsers);
            foreach (var user in usersList)
            {
                try
                {
                    user.Value.ReceiveMessage("Lobby", fromUser, message);
                }
                catch (CommunicationObjectAbortedException ex)
                {
                    Console.WriteLine($"Aborted connection with {user.Key}: {ex.Message}");
                    ConnectedUsers.TryRemove(user.Key, out _);
                }
                catch (CommunicationException ex)
                {
                    Console.WriteLine($"Communication Error with {user.Key}: {ex.Message}");
                    ConnectedUsers.TryRemove(user.Key, out _);
                }
                catch (TimeoutException ex)
                {
                    Console.WriteLine($"Timeout to send notification {user.Key}: {ex.Message}");
                    ConnectedUsers.TryRemove(user.Key, out _);
                }
                catch (ObjectDisposedException ex)
                {
                    Console.WriteLine($"Channel closed for {user.Key}: {ex.Message}");
                    ConnectedUsers.TryRemove(user.Key, out _);
                }
            }
        }

        private void NotifySender(string username, string notification)
        {
            if(ConnectedUsers.TryGetValue(username, out var senderCallback))
            {
                try
                {
                    senderCallback.ReceiveSystemNotification(notification);
                }
                catch (CommunicationException ex)
                {
                    Console.WriteLine($"Couldn't notify {username}: {ex.Message}");
                }
                catch (TimeoutException ex)
                {
                    Console.WriteLine($"Timeout while notifying {username}: {ex.Message}");
                }
            }
        }
    }
}
