using ArchsVsDinosServer.Interfaces.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic
{
    public class GameLogic : IGameLogic
    {
        Task<bool> IGameLogic.InitializeMatch(string lobbyCode, List<string> players)
        {
            return Task.FromResult(true);
        }
    }
}
