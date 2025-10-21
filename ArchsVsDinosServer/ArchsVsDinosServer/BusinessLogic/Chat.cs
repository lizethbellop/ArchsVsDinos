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
        private static readonly ConcurrentDictionary<string, IChatManagerCallback> RoomMembers = new ConcurrentDictionary<string, IChatManagerCallback>();
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

        public void SendMessageToRoom(string roomId, string message)
        {
            string senderUsername = null;

            try
            {
                senderUsername = GetUsernameFromCallback();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Error identifying the user: {ex.Message}");
                throw new FaultException("No se pudo identificar al remitente");
            }

            if (string.IsNullOrEmpty(senderUsername))
            {
                Console.WriteLine("Sender couldn't be identified");
                throw new FaultException("Usuario no atenticado");
            }

            BroadcastMessageToAll(senderUsername, message);
        }

        public void SendMessageToUser(string targetUser, string message)
        {
            string senderUsername = null;
            IChatManagerCallback senderCallback = null;

            try
            {
                senderUsername = GetUsernameFromCallback();
                senderCallback = OperationContext.Current.GetCallbackChannel<IChatManagerCallback>();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Error identifying the user: {ex.Message}");
                throw new FaultException("No se pudo identificar al remitente");
            }

            if (string.IsNullOrEmpty(senderUsername))
            {
                Console.WriteLine("Sender couldn't be identified");
                throw new FaultException("Usuario no atenticado");
            }

            if(!ConnectedUsers.TryGetValue(targetUser, out var targetCallback))
            {
                try
                {
                    senderCallback.ReceiveSystemNotification($"El usuario '{targetUser}' no está conectado");
                }
                catch (CommunicationException ex)
                {
                    Console.WriteLine($"Communication error with sender: {ex.Message}");
                }
                catch (TimeoutException ex)
                {
                    Console.WriteLine($"Timeout while notyfing sender: {ex.Message}");

                }
                return;
            }

            Console.WriteLine($"[Privado] {senderUsername} -> {targetUser}: {message}");

            try
            {
                targetCallback.ReceiveMessage("PRIVADO", senderUsername, message);
            }
            catch (CommunicationObjectAbortedException ex)
            {
                Console.WriteLine($"Aborted connection con {targetUser}: {ex.Message}");
                ConnectedUsers.TryRemove(targetUser, out _);

                try
                {
                    senderCallback.ReceiveSystemNotification($"El usuario '{targetUser}' se desconectó inesperadamente");
                }
                catch (CommunicationException)
                {
                    
                }
            }
            catch (CommunicationException ex)
            {
                Console.WriteLine($"Communication error with {targetUser}: {ex.Message}");
                ConnectedUsers.TryRemove(targetUser, out _);

                try
                {
                    senderCallback.ReceiveSystemNotification($"Message couldn't be sent");
                }
                catch (CommunicationException)
                {

                }
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine($"Timeout while trying to send the message {targetUser}: {ex.Message}");

                try
                {
                    senderCallback.ReceiveSystemNotification($"Timeout al enviar mensaje a '{targetUser}'");
                }
                catch (CommunicationException)
                {
                    
                }
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

        private string GetUsernameFromCallback()
        {
            if (OperationContext.Current == null)
            {
                throw new InvalidOperationException("There is no operation context available");
            }

            IChatManagerCallback currentCallback = OperationContext.Current.GetCallbackChannel<IChatManagerCallback>();

            if (currentCallback == null)
            {
                throw new InvalidOperationException("Thecallback channel couldn't be obtained");
            }

            List<KeyValuePair<string, IChatManagerCallback>> usersList = new List<KeyValuePair<string, IChatManagerCallback>>(ConnectedUsers);
            foreach (var user in usersList)
            {
                if (ReferenceEquals(user.Value, currentCallback))
                {
                    return user.Key;
                }
            }

            return null;
        }
    }
}
