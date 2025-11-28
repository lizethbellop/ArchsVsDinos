using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Logging
{
    public class Logger : ILogger
    {
        private readonly ILog log;

        public Logger(Type type = null)
        {
            log = LogManager.GetLogger(type ?? typeof(Logger));
        }

        public void LogInfo(string message)
        {
            log.Info(message);
        }

        public void LogWarning(string message)
        {
            log.Warn(message);
        }

        public void LogError(string message, Exception ex = null)
        {
            if (ex != null)
            {
                log.Error(message, ex);
            }
            else
            {
                log.Error(message);
            }
        }

        public void LogDebug(string message)
        {
            log.Debug(message);
        }
    }
}
