using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Services;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICodeGenerator = ArchsVsDinosServer.Interfaces.ICodeGenerator;

namespace ArchsVsDinosServer.Utils
{
    public class RegisterServiceDependencies
    {
        public ISecurityHelper securityHelper { get; set; }
        public ILoggerHelper loggerHelper { get; set; }
        public IEmailService emailService { get; set; }
        public ICodeGenerator codeGenerator { get; set; }
        public IVerificationCodeManager codeManager { get; set; }
        public Func<IDbContext> contextFactory { get; set; }

        public RegisterServiceDependencies()
        {
            securityHelper = new Wrappers.SecurityHelperWrapper();
            loggerHelper = new Wrappers.LoggerHelperWrapper();
            emailService = new EmailService();
            codeGenerator = new Wrappers.CodeGeneratorWrapper();
            codeManager = new VerificationCodeManager();
            contextFactory = () => new Wrappers.DbContextWrapper();
        }
    }
}
