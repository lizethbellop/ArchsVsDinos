using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Wrappers
{
    public class EmailServiceWrapper : IEmailService
    {
        public bool SendVerificationEmail(string email, string verificationCode)
        {
            try
            {
                EmailService.SendVerificationEmail(email, verificationCode);
                return true;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError($"Error sending verification email", ex);
                return false;
            }
        }
    }
}
