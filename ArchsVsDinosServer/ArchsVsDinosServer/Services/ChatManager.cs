using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Services.Interfaces;
using ArchsVsDinosServer.Utils;
using ArchsVsDinosServer.Wrappers;
using Contracts;
using Contracts.DTO;
using Contracts.DTO.Result_Codes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class ChatManager : IChatManager
    {
        private Chat ChatBusinessLogic;
        private static ILobbyNotifier lobbyNotifier;
        private static IGameNotifier gameNotifier;
        private ILoggerHelper loggerHelper = new LoggerHelperWrapper();

        public static void RegisterNotifiers(ILobbyNotifier lobby, IGameNotifier game)
        {
            lobbyNotifier = lobby;
            gameNotifier = game;
        }

        public ChatManager()
        {
            var dependencies = new BasicServiceDependencies
            {
                loggerHelper = loggerHelper,
                contextFactory = () => new DbContextWrapper()
            };

            ChatBusinessLogic = new Chat(
                dependencies,
                lobbyNotifier,
                gameNotifier
            );
        }

        public void Connect(ChatConnectionRequest request)
        {
            ChatBusinessLogic.Connect(request);
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
