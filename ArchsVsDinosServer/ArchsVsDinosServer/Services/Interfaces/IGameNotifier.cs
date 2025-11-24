using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services.Interfaces
{
    public interface IGameNotifier
    {
        void NotifyPlayerExpelled(string matchCode, string username, string reason);
        void NotifyGameClosure(string matchCode, string reason);
    }
}
