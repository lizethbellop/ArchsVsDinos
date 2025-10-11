using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Wrappers
{
    public class LoggerHelperWrapper : ILoggerHelper
    {
        public void LogError(string message, Exception ex)
        {
            LoggerHelper.LogError(message, ex);
        }

        public void LogWarning(string message)
        {
            LoggerHelper.LogWarn(message);
        }
    }
}
