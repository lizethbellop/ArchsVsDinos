using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement
{
    public class GameCoreContext
    {
        public GameSessionManager Sessions { get; }
        public GameSetupHandler Setup { get; }

        public GameCoreContext(
            GameSessionManager sessions,
            GameSetupHandler setup)
        {
            Sessions = sessions;
            Setup = setup;
        }
    }

}
