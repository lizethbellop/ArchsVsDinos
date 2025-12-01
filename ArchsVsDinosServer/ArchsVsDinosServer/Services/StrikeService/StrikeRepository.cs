using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services.StrikeService
{
    public class StrikeRepository
    {
        private readonly ILoggerHelper logger;
        private const int StrikeExpirationDays = 30;

        public StrikeRepository(ILoggerHelper logger)
        {
            this.logger = logger;
        }

        public int GetActiveStrikes(IDbContext context, int userId)
        {
            try
            {
                DateTime now = DateTime.UtcNow;
                return context.UserHasStrike
                    .Where(u => u.idUser == userId)
                    .Join(context.Strike,
                          u => u.idStrike,
                          s => s.idStrike,
                          (u, s) => s)
                    .Count(s => s.endDate > now);
            }
            catch (Exception ex)
            {
                logger.LogError($"GetActiveStrikes: Error for user {userId}", ex);
                return 0;
            }
        }

        public StrikeKind GetStrikeKind(IDbContext context, string typeName)
        {
            try
            {
                return context.StrikeKind.FirstOrDefault(s => s.name == typeName);
            }
            catch (Exception ex)
            {
                logger.LogError($"GetStrikeKind: Error for strike type {typeName}", ex);
                return null;
            }
        }

        public void AddStrike(IDbContext context, int userId, StrikeKind kind, string reason)
        {
            try
            {
                var strike = new Strike
                {
                    startDate = DateTime.UtcNow,
                    endDate = DateTime.UtcNow.AddDays(StrikeExpirationDays),
                    idStrikeKind = kind.idStrikeKind
                };

                context.Strike.Add(strike);
                context.SaveChanges();

                var userStrike = new UserHasStrike
                {
                    idUser = userId,
                    idStrike = strike.idStrike,
                    strikeDate = DateTime.UtcNow
                };

                context.UserHasStrike.Add(userStrike);
                context.SaveChanges();

                logger.LogInfo($"Strike added for user {userId}: {kind.name} - {reason}");
            }
            catch (DbUpdateException ex)
            {
                logger.LogError($"AddStrike: DB error for user {userId}", ex);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError($"AddStrike: Unexpected error for user {userId}", ex);
                throw;
            }
        }

        public List<StrikeInfo> GetHistory(IDbContext context, int userId)
        {
            try
            {
                DateTime now = DateTime.UtcNow;
                return context.UserHasStrike
                    .Where(uhs => uhs.idUser == userId)
                    .Join(context.Strike, uhs => uhs.idStrike, s => s.idStrike, (uhs, s) => new { uhs, s })
                    .Join(context.StrikeKind, x => x.s.idStrikeKind, sk => sk.idStrikeKind,
                        (x, sk) => new StrikeInfo
                        {
                            StrikeId = x.s.idStrike,
                            StrikeType = sk.name,
                            StrikeDate = x.uhs.strikeDate ?? DateTime.MinValue,
                            ExpirationDate = x.s.endDate,
                            IsActive = x.s.endDate > now
                        })
                    .OrderByDescending(s => s.StrikeDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                logger.LogError($"GetHistory: Error for user {userId}", ex);
                return new List<StrikeInfo>();
            }
        }
    }

}
