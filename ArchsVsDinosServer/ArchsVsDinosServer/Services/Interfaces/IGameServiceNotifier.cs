using ArchsVsDinosServer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services.Interfaces
{
    public interface IGameServiceNotifier
    {
        void NotifyPlayerExpelled(string matchCode, int userId, string reason);
        void NotifyGameClosure(string matchCode, GameEndType gameType, string reason);
    }

}
