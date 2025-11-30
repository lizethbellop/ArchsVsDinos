using ArchsVsDinosServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services.StrikeService
{
    public class UserFetcher
    {
        private readonly ILoggerHelper logger;

        public UserFetcher(ILoggerHelper logger)
        {
            this.logger = logger;
        }

        public UserAccount GetUser(IDbContext context, int userId, string method)
        {
            try
            {
                var user = context.UserAccount.FirstOrDefault(u => u.idUser == userId);
                if (user == null)
                    logger.LogWarning($"{method}: User {userId} not found");
                return user;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError($"{method}: Invalid operation for userId {userId}", ex);
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError($"{method}: Unexpected error for userId {userId}", ex);
                return null;
            }
        }
    }

}
