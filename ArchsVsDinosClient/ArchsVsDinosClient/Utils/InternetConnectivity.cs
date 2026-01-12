using System.Net.NetworkInformation;

namespace ArchsVsDinosClient.Utils
{
    internal static class InternetConnectivity
    {
        internal static bool HasInternet()
        {
            return NetworkInterface.GetIsNetworkAvailable();
        }
    }
}
