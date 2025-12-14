using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement
{
    public class LobbyCoreContext
    {
        public LobbySession Session { get; }
        public LobbyValidationHelper Validation { get; }
        public LobbyCodeGeneratorHelper CodeGenerator { get; }

        public LobbyCoreContext(
            LobbySession session,
            LobbyValidationHelper validation,
            LobbyCodeGeneratorHelper codeGenerator)
        {
            Session = session;
            Validation = validation;
            CodeGenerator = codeGenerator;
        }
    }

}
