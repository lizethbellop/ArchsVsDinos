using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Logging
{
    public interface ILogger
    {
        void LogError(string message, Exception exception = null);
        void LogWarning(string message);
        void LogInfo(string message);
    }
}
