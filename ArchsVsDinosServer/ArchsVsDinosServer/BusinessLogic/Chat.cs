using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class Chat : IChatManager
    {
        public void Connect(string username)
        {
            throw new NotImplementedException();
        }

        public void Disconnect(string username)
        {
            throw new NotImplementedException();
        }

        public void SendMessageToRoom(string roomId, string message)
        {
            throw new NotImplementedException();
        }

        public void SendMessageToUser(string targetUser, string message)
        {
            throw new NotImplementedException();
        }
    }
}
