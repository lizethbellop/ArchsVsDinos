using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Utils
{
    public class BasicServiceDependencies
    {
        public ILoggerHelper loggerHelper { get; set; }
        public Func<IDbContext> contextFactory { get; set; }
        public ICallbackProvider callbackProvider { get; set; }

        public BasicServiceDependencies()
        {
            loggerHelper = new Wrappers.LoggerHelperWrapper();
            contextFactory = () => new Wrappers.DbContextWrapper();
            callbackProvider = new WcfCallbackProvider();
        }
    }
}
