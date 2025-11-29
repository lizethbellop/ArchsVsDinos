using ArchsVsDinosServer.Services;
using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Host
{
    internal class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        static void Main(string[] args)
        {

            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            log.Info("=== Iniciando servidor ArchsVsDinos ===");

            using (ServiceHost registerHost = new ServiceHost(typeof(RegisterManager)))
            using (ServiceHost authenticationHost = new ServiceHost(typeof(AuthenticationManager)))
            using (ServiceHost profileHost = new ServiceHost(typeof(ProfileManager)))
            using (ServiceHost chatHost = new ServiceHost(typeof(ChatManager)))
            using (ServiceHost lobbyHost = new ServiceHost(typeof(LobbyManager)))
            using (ServiceHost friendHost = new ServiceHost(typeof(FriendManager)))
            using(ServiceHost friendRequestHost = new ServiceHost(typeof(FriendRequestManager)))
            using (ServiceHost gameHost = new ServiceHost(typeof(GameManager)))
            using (ServiceHost statisticsHost = new ServiceHost(typeof(StatisticsManager)))
            {
                try
                {
                    registerHost.Open();
                    authenticationHost.Open();
                    profileHost.Open();
                    chatHost.Open();
                    lobbyHost.Open();
                    friendHost.Open();
                    friendRequestHost.Open();
                    gameHost.Open();
                    statisticsHost.Open();
                    Console.WriteLine("Server is running");
                    Console.ReadLine();
                }
                catch (CommunicationException ex)
                {
                    Console.WriteLine("Error starting services: ", ex.Message);
                    Console.WriteLine(ex.ToString());

                    registerHost.Abort();
                    authenticationHost.Abort();
                    profileHost.Abort();
                    chatHost.Abort();
                    lobbyHost.Abort();
                    gameHost.Abort();
                }

            }

        }

    }
}
