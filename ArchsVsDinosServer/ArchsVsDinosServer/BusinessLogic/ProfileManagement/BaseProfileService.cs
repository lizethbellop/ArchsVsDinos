using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.ProfileManagement
{
    public class BaseProfileService
    {
        protected readonly IValidationHelper validationHelper;
        protected readonly Func<IDbContext> contextFactory;
        protected readonly ILoggerHelper loggerHelper;
        protected readonly ISecurityHelper securityHelper;

        public BaseProfileService(Func<IDbContext> _contextFactory, IValidationHelper _validationHelper, ILoggerHelper _loggerHelper, ISecurityHelper _securityHelper)
        {
            validationHelper = _validationHelper;
            contextFactory = _contextFactory;
            loggerHelper = _loggerHelper;
            securityHelper = _securityHelper;
        }

        public BaseProfileService() : this(() => new DbContextWrapper(),
            new Wrappers.ValidationHelperWrapper(), new Wrappers.LoggerHelperWrapper(), new Wrappers.SecurityHelperWrapper())
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
