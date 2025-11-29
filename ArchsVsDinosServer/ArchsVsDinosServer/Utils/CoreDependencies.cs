using ArchsVsDinosServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Utils
{
    public class CoreDependencies
    {
        public ISecurityHelper securityHelper { get; set; }
        public IValidationHelper validationHelper { get; set; }
        public ILoggerHelper loggerHelper { get; set; }

        public CoreDependencies(
            ISecurityHelper securityHelper,
            IValidationHelper validationHelper,
            ILoggerHelper loggerHelper)
        {
            this.securityHelper = securityHelper;
            this.validationHelper = validationHelper;
            this.loggerHelper = loggerHelper;
        }
    }
}
