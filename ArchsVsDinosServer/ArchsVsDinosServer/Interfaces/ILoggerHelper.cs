using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Interfaces
{
    public interface ILoggerHelper
    {
        void LogError(string message, Exception ex);
        void LogWarning(string message);
        void LogInfo(string message);

        void LogDebug(string message);
    }
}
