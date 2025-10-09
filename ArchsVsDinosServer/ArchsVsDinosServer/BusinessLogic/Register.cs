using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic
{
    internal class Register
    {
        public bool RegisterManager(UserAccountDTO userAccountDTO)
        {
            try
            {
                using (var scope = new TransactionScope())
                using (var context = new ArchsVsDinosConnection())
                {

                    var player = InitialConfig.InitialPlayer;
                    context.Player.Add(player);
                    context.SaveChanges();

                    var configuration = InitialConfig.InitialConfiguration;
                    context.Configuration.Add(configuration);
                    context.SaveChanges();

                    var userAccount = new UserAccount
                    {
                        email = userAccountDTO.Email,
                        password = SecurityHelper.HashPassword(userAccountDTO.Password),
                        name = userAccountDTO.Name,
                        username = userAccountDTO.Username,
                        nickname = userAccountDTO.Nickname,
                        idConfiguration = configuration.IdConfiguration,
                        idPlayer = player.IdPlayer
                    }

                    context.UserAccount.Add(userAccount);
                    context.SaveChanges();

                    return true;
                }
            }
            catch (EntityException ex)
            {
                LoggerHelper.LogError($"Database connection error at Register", ex);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Register: {ex.Message}");
                return false;
            }
        }

    }
}
}
