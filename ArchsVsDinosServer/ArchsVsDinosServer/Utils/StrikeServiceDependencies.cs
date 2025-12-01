using ArchsVsDinosServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Utils
{
    public class StrikeServiceDependencies
    {
        public ILoggerHelper Logger { get; set; }
        public Func<IDbContext> ContextFactory { get; set; }
        public ProfanityFilter ProfanityFilter { get; set; }

        public StrikeServiceDependencies()
        {
            Logger = new Wrappers.LoggerHelperWrapper();
            ContextFactory = () => new Wrappers.DbContextWrapper();
            ProfanityFilter = new ProfanityFilter();
        }
    }

}
