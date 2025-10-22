using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Wrappers
{
    public class SecurityHelperWrapper : ISecurityHelper
    {
        public string HashPassword(string password)
        {
            return SecurityHelper.HashPassword(password);
        }

        public bool VerifyPassword(string plainPassword, string hashedPasswordFromDB)
        {
            return SecurityHelper.VerifyPassword(plainPassword, hashedPasswordFromDB);
        }
    }
}
