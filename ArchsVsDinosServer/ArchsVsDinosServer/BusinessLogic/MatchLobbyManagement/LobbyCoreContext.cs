using ArchsVsDinosServer.Interfaces.Lobby;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement
{
    public class LobbyCoreContext
    {
        public ILobbySession Session { get; }
        public ILobbyValidationHelper Validation { get; }
        public ILobbyCodeGeneratorHelper CodeGenerator { get; }

        public LobbyCoreContext(
            ILobbySession session,
            ILobbyValidationHelper validation,
            ILobbyCodeGeneratorHelper codeGenerator)
        {
            Session = session;
            Validation = validation;
            CodeGenerator = codeGenerator;
        }
    }


}
