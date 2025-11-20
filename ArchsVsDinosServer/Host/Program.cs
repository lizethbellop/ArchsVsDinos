using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using ArchsVsDinosServer.Services;

namespace Host
{
    internal class Program
    {
        static void Main(string[] args)
        {

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
