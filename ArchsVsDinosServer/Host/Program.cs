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
            {
                try
                {
                    registerHost.Open();
                    authenticationHost.Open();
                    profileHost.Open();
                    Console.WriteLine("Server is running");
                    Console.ReadLine();
                }
                catch (CommunicationException ce)
                {
                    Console.WriteLine("Error starting services: ", ce.Message);
                    Console.WriteLine(ce.ToString());

                    registerHost.Abort();
                    authenticationHost.Abort();
                    profileHost.Abort();
                }

            }

        }

    }
}
