using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchsVsDinosServer.Utils;

namespace ArchsVsDinosServer.BusinessLogic.ProfileManagement
{
    public class BaseProfileService
    {
        protected readonly IValidationHelper validationHelper;
        protected readonly Func<IDbContext> contextFactory;
        protected readonly ILoggerHelper loggerHelper;
        protected readonly ISecurityHelper securityHelper;

        public BaseProfileService(ServiceDependencies dependencies)
        {
            validationHelper = dependencies.validationHelper;
            contextFactory = dependencies.contextFactory;
            loggerHelper = dependencies.loggerHelper;
            securityHelper = dependencies.securityHelper;
        }

        public BaseProfileService() : this(new ServiceDependencies())
        {
        }


        protected IDbContext GetContext()
        {
            return contextFactory();
        }

        protected bool IsEmpty(string value)
        {
            return validationHelper.IsEmpty(value);
        }

        protected bool UpdateIsEmpty(string value1, string value2)
        {
            return validationHelper.IsEmpty(value1) || validationHelper.IsEmpty(value2);
        }
    }
}
