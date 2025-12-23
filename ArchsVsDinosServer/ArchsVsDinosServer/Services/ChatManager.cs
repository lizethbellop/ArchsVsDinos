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
        private static ILobbyServiceNotifier lobbyNotifier;
        private static IGameServiceNotifier gameNotifier;
        private static IModerationManager moderationManager;
        private ILoggerHelper loggerHelper = new LoggerHelperWrapper();

        public static void RegisterNotifiers(
            ILobbyServiceNotifier lobby,
            IGameServiceNotifier game,
            IModerationManager moderation)
        {
            lobbyNotifier = lobby;
            gameNotifier = game;
            moderationManager = moderation;
        }

        public ChatManager()
        {
            if (moderationManager == null)
            {
                moderationManager = new ModerationManager();
            }

            var dependencies = new ChatServiceDependencies
            {
                LoggerHelper = loggerHelper,
                ContextFactory = () => new DbContextWrapper(),
                CallbackProvider = new CallbackProviderWrapper(),
                ModerationManager = moderationManager
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

    public class CallbackProviderWrapper : ICallbackProvider
    {
        public IChatManagerCallback GetCallback()
        {
            return OperationContext.Current.GetCallbackChannel<IChatManagerCallback>();
        }
    }
}
