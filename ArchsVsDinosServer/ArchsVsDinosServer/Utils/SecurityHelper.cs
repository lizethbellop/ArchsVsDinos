using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;

namespace ArchsVsDinosServer.Utils
{
    public class SecurityHelper
    {

        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        public static bool VerifyPassword(string plainPassword, string hashedPasswordFromDB)
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, hashedPasswordFromDB);
        }
    }
}
