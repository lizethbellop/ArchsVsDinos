using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Logging
{
    public class Logger : ILogger
    {
        public void LogError(string message, Exception exception = null)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] {message}");
            if (exception != null)
            {
                System.Diagnostics.Debug.WriteLine($"Exception: {exception.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {exception.StackTrace}");
            }
        }

        public void LogWarning(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[WARNING] {message}");
        }

        public void LogInfo(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[INFO] {message}");
        }

        public void LogDebug(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] {message}");
        }
    }
}
