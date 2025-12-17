using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services.Interfaces
{
    public interface ILobbyServiceNotifier
    {
        void NotifyPlayerExpelled(string lobbyCode, int userId, string reason);
        void NotifyLobbyClosure(string lobbyCode, string reason);
    }


}
