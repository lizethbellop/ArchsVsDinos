using ArchsVsDinosServer.Interfaces.Lobby;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchsVsDinosServer.Utils;

namespace ArchsVsDinosServer.BusinessLogic.MatchLobbyManagement
{
    public class LobbyCodeGeneratorHelper : ILobbyCodeGeneratorHelper
    {
        private const int CodeLength = 5;
        private const int MaxAttempts = 100;
        public string GenerateLobbyCode(Func<string, bool> existsPredicate)
        {
            if (existsPredicate == null)
            {
                throw new ArgumentNullException(nameof(existsPredicate));
            }

            for(int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                string code = SafeCodeGenerator.GenerateRandomString(CodeLength);
                if (!existsPredicate(code))
                {
                    return code;
                }
            }

            throw new InvalidOperationException("Unable to generate a unique lobby code after maximum attempts.");
        }
    }
}
