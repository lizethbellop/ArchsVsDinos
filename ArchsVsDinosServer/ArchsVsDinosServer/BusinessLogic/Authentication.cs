using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic
{
    public class Authentication
    {
        public static bool Login(string username, string password)
        {
            try
            {
                using (var context = new ArchsVsDinosConnection())
                {
                    string passwordHash = SecurityHelper.HashPassword(password);

                    var user = context.UserAccount.FirstOrDefault(u => u.username == username && u.password == passwordHash);
                    return user != null;
                }
            }
            catch (EntityException ex)
            {
                LoggerHelper.LogError($"Database connection error at Login for user: {username}", ex);
                return false;
            }
            catch (ArgumentException ex)
            {
                LoggerHelper.LogWarn("Error while hashing the password");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en Login: {ex.Message}");
                return false;
            }
        }
    }
}
