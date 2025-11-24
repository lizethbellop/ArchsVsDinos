using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services.Interfaces
{
    public interface ILobbyNotifier
    {
        void NotifyPlayerExpelled(string username, string reason);
        void NotifyLobbyClosure(string reason);
    }
}
