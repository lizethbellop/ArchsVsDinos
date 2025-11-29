using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchsVsDinosServer.Interfaces;

namespace ArchsVsDinosServer.Utils
{
    public class ServiceDependencies
    {
        public ISecurityHelper securityHelper { get; set; }
        public IValidationHelper validationHelper { get; set; }
        public ILoggerHelper loggerHelper { get; set; }
        public Func<IDbContext> contextFactory { get; set; }

        public ServiceDependencies()
        {
            securityHelper = new Wrappers.SecurityHelperWrapper();
            validationHelper = new Wrappers.ValidationHelperWrapper();
            loggerHelper = new Wrappers.LoggerHelperWrapper();
            contextFactory = () => new Wrappers.DbContextWrapper();
        }


        public ServiceDependencies(
            ISecurityHelper securityHelper,
            IValidationHelper validationHelper,
            ILoggerHelper loggerHelper,
            Func<IDbContext> contextFactory)
        {
            this.securityHelper = securityHelper;
            this.validationHelper = validationHelper;
            this.loggerHelper = loggerHelper;
            this.contextFactory = contextFactory;
        }

        public ServiceDependencies(CoreDependencies coreDeps, Func<IDbContext> contextFactory)
        {
            this.securityHelper = coreDeps.securityHelper;
            this.validationHelper = coreDeps.validationHelper;
            this.loggerHelper = coreDeps.loggerHelper;
            this.contextFactory = contextFactory;
        }
    }
}
