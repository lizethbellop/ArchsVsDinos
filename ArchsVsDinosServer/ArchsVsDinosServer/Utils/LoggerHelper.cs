using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Utils
{
    public class LoggerHelper
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(LoggerHelper));

        public static void LogInfo(string message)
        {
            log.Info(message);
        }

        public static void LogWarn(string message) 
        { 
            log.Warn(message); 
        }

        public static void LogError(string message, Exception ex) 
        {
            if(ex != null)
            {
                log.Error(message, ex);
            }
            else
            {
                log.Error(message);
            }
        }

        public static void LogFatal(string message, Exception ex)
        {
            if (ex != null)
            {
                log.Fatal(message, ex);
            }
            else
            {
                log.Fatal(message);
            }
        }

        public static void LogDebug(string message)
        {
            log.Debug(message);
        }
    }
}
