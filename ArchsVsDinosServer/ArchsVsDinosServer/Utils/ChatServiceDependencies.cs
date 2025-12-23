using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Services.Interfaces;
using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Utils
{
    public class ChatServiceDependencies
    {
        public ILoggerHelper LoggerHelper { get; set; }
        public Func<IDbContext> ContextFactory { get; set; }
        public ICallbackProvider CallbackProvider { get; set; }
        public IModerationManager ModerationManager { get; set; }
    }
}
