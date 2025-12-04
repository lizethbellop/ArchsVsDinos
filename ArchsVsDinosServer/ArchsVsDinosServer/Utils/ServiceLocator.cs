using ArchsVsDinosServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Utils
{
    public static class ServiceLocator
    {
        private static IVerificationCodeManager codeManager;
        private static readonly object _lock = new object();

        public static IVerificationCodeManager GetCodeManager(ILoggerHelper logger)
        {
            if (codeManager == null)
            {
                lock (_lock)
                {
                    if (codeManager == null)
                    {
                        codeManager = new VerificationCodeManager(logger);
                    }
                }
            }
            return codeManager;
        }

        public static void SetCodeManager(IVerificationCodeManager manager)
        {
            codeManager = manager;
        }

        public static void Reset()
        {
            codeManager = null;
        }
    }
}
