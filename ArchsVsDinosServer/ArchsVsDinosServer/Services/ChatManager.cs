using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using ArchsVsDinosServer.BusinessLogic;
using Contracts.DTO.Result_Codes;

namespace ArchsVsDinosServer.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class ChatManager : IChatManager
    {
        private Chat chatBusinessLogic;

        public ChatManager()
        {
            chatBusinessLogic = new Chat();
        }

        public ChatResultCode Connect(string username)
        {
            throw new NotImplementedException();
        }

        public ChatResultCode Disconnect(string username)
        {
            throw new NotImplementedException();
        }

        public List<string> GetConnectedUsers()
        {
            throw new NotImplementedException();
        }

        public void SendMessageToRoom(string message, string username)
        {
            chatBusinessLogic.SendMessageToRoom(message, username);
        }

        
    }
}
