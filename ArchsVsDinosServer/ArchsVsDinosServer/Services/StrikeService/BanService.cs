using ArchsVsDinosServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services.StrikeService
{
    public class BanService
    {
        private readonly ILoggerHelper logger;
        private const int StrikeLimit = 3;

        public BanService(ILoggerHelper logger)
        {
            this.logger = logger;
        }

        public bool SyncBanStatus(IDbContext context, UserAccount user, int activeStrikes)
        {
            try
            {
                bool shouldBeBanned = activeStrikes >= StrikeLimit;

                if (user.isBanned != shouldBeBanned)
                {
                    user.isBanned = shouldBeBanned;
                    context.SaveChanges();
                    logger.LogInfo($"Ban status updated for {user.username}: {shouldBeBanned}");
                }

                return shouldBeBanned;
            }
            catch (DbUpdateException ex)
            {
                logger.LogError($"SyncBanStatus: DB error for {user.username}", ex);
                return user.isBanned;
            }
            catch (Exception ex)
            {
                logger.LogError($"SyncBanStatus: Unexpected error for {user.username}", ex);
                return user.isBanned;
            }
        }

        public void BanUser(IDbContext context, UserAccount user)
        {
            try
            {
                user.isBanned = true;
                context.SaveChanges();
                logger.LogInfo($"User {user.username} banned");
            }
            catch (DbUpdateException ex)
            {
                logger.LogError($"BanUser: DB error for {user.username}", ex);
            }
            catch (Exception ex)
            {
                logger.LogError($"BanUser: Unexpected error for {user.username}", ex);
            }
        }

        public void UnbanIfExpired(IDbContext context, UserAccount user, int activeStrikes)
        {
            try
            {
                if (user.isBanned && activeStrikes < StrikeLimit)
                {
                    user.isBanned = false;
                    context.SaveChanges();
                    logger.LogInfo($"User {user.username} unbanned");
                }
            }
            catch (DbUpdateException ex)
            {
                logger.LogError($"UnbanIfExpired: DB error for {user.username}", ex);
            }
            catch (Exception ex)
            {
                logger.LogError($"UnbanIfExpired: Unexpected error for {user.username}", ex);
            }
        }
    }

}
