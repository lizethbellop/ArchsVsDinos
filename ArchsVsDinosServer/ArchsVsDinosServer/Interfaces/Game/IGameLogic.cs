using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Interfaces.Game
{
    public interface IGameLogic
    {
        Task<bool> InitializeMatch(string lobbyCode, List<string> players);
    }
}
