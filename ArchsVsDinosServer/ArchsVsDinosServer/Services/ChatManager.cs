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
        private Chat ChatBusinessLogic;

        public ChatManager()
        {
            var loggerHelper = new Wrappers.LoggerHelperWrapper();
            ChatBusinessLogic = new Chat(loggerHelper);
        }

        public void Connect(string username)
        {
            ChatBusinessLogic.Connect(username);
        }


        public void SendMessageToRoom(string message, string username)
        {
            ChatBusinessLogic.SendMessageToRoom(message, username);
        }

        public void Disconnect(string username)
        {
            ChatBusinessLogic.Disconnect(username);
        }
    }
}
