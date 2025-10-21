using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using ArchsVsDinosServer.BusinessLogic;

namespace ArchsVsDinosServer.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class ChatManager : IChatManager
    {
        private Chat chatBusinessLogic;
        public void Connect(string username)
        {
            chatBusinessLogic.Connect(username);
        }

        public void Disconnect(string username)
        {
            chatBusinessLogic.Disconnect(username);
        }

        public void SendMessageToRoom(string message, string username)
        {
            chatBusinessLogic.SendMessageToRoom(message, username);
        }

        public void SendMessageToUser(string username, string targetUser, string message)
        {
            chatBusinessLogic.SendMessageToUser(username, targetUser, message);
        }
    }
}
