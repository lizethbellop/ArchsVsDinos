using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Interfaces.Lobby
{
    public interface ILobbyCodeGeneratorHelper
    {
        string GenerateLobbyCode(Func<string, bool> existsPredicate);
    }
}
