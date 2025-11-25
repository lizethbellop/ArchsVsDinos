using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    public class StrikeManager
    {
        private readonly ProfanityFilter profanityFilter;
        private readonly ILoggerHelper loggerHelper;
        private readonly Func<IDbContext> contextFactory;

        private const int StrikeLimit = 3;
        private const int StrikeExpirationDays = 30;
        private const int SpamMessageThreshold = 5; 
        private const int SpamTimeWindowSeconds = 10; 

        private readonly ConcurrentDictionary<int, List<DateTime>> userMessageTimestamps;

        public StrikeManager(ServiceDependencies dependencies, ProfanityFilter profanityFilter)
        {
            loggerHelper = dependencies.loggerHelper;
            contextFactory = dependencies.contextFactory;
            this.profanityFilter = profanityFilter;
            userMessageTimestamps = new ConcurrentDictionary<int, List<DateTime>>();
        }

        public StrikeManager() : this(new ServiceDependencies(), new ProfanityFilter())
        {
        }

        public bool CanSendMessage(int userId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return true;
            }

            try
            {
                using (var context = contextFactory())
                {
                    var user = context.UserAccount.FirstOrDefault(u => u.idUser == userId);

                    if (user == null)
                    {
                        loggerHelper.LogWarning($"CanSendMessage: User with ID {userId} not found");
                        return false;
                    }

                    int activeCount = GetActiveStrikeCount(context, user);
                    if (activeCount >= StrikeLimit)
                    {
                        loggerHelper.LogInfo($"User {user.username} is banned ({activeCount} active strikes)");

                        if (!user.isBanned)
                        {
                            user.isBanned = true;
                            context.SaveChanges();
                        }

                        return false;
                    }
                    else
                    {
                        if (user.isBanned)
                        {
                            user.isBanned = false;
                            context.SaveChanges();
                            loggerHelper.LogInfo($"User {user.username} unbanned (strikes expired)");
                        }
                    }

                    if (IsSpamming(userId))
                    {
                        AddStrikeByType(context, user, "Spam", "Sending messages too quickly");
                        loggerHelper.LogWarning($"User {user.username} detected spamming");
                        return false;
                    }

                    return HandleProfanityCheck(context, user, message);
                }
            }
            catch (ArgumentNullException ex)
            {
                loggerHelper.LogError($"CanSendMessage: Null argument for userId {userId}", ex);
                return false;
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"CanSendMessage: Invalid operation for userId {userId}", ex);
                return false;
            }
            catch (DbUpdateException ex)
            {
                loggerHelper.LogError($"CanSendMessage: Database update error for userId {userId}", ex);
                return false;
            }
            catch (DataException ex)
            {
                loggerHelper.LogError($"CanSendMessage: Data access error for userId {userId}", ex);
                return false;
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"CanSendMessage: Unexpected error for userId {userId} - {ex.Message}", ex);
                return false;
            }
        }

        public bool AddManualStrike(int userId, string strikeTypeName, string reason)
        {
            try
            {
                using (var context = contextFactory())
                {
                    var user = context.UserAccount.FirstOrDefault(u => u.idUser == userId);

                    if (user == null)
                    {
                        loggerHelper.LogWarning($"AddManualStrike: User {userId} not found");
                        return false;
                    }

                    var success = AddStrikeByType(context, user, strikeTypeName, reason);

                    if (success)
                    {
                        int activeCount = GetActiveStrikeCount(context, user);
                        if (activeCount >= StrikeLimit)
                        {
                            BanUser(context, user);
                        }

                        loggerHelper.LogInfo($"Manual strike added to {user.username}: {strikeTypeName} - {reason}");
                    }

                    return success;
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                loggerHelper.LogWarning($"AddManualStrike: Concurrency conflict for userId {userId}");
                return false;
            }
            catch (DbUpdateException ex)
            {
                loggerHelper.LogError($"AddManualStrike: Database update error for userId {userId}", ex);
                return false;
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"AddManualStrike: Invalid operation for userId {userId}", ex);
                return false;
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"CanSendMessage: SQL Server error for userId {userId}", ex);
                return false;
            }
            catch (Exception)
            {
                loggerHelper.LogInfo($"AddManualStrike: Unexpected error for userId {userId}");
                return false;
            }
        }

        public bool IsUserBanned(int userId)
        {
            try
            {
                using (var context = contextFactory())
                {
                    var user = context.UserAccount.FirstOrDefault(u => u.idUser == userId);
                    if (user == null)
                    {
                        loggerHelper.LogWarning($"IsUserBanned: User {userId} not found");
                        return false;
                    }

                    int activeCount = GetActiveStrikeCount(context, user);
                    bool isBanned = activeCount >= StrikeLimit;

                    if (user.isBanned != isBanned)
                    {
                        user.isBanned = isBanned;
                        context.SaveChanges();
                        loggerHelper.LogInfo($"Updated ban status for user {user.username}: {isBanned}");
                    }

                    return isBanned;
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                loggerHelper.LogWarning($"IsUserBanned: Concurrency conflict for userId {userId}");
                return false;
            }
            catch (DbUpdateException ex)
            {
                loggerHelper.LogError($"IsUserBanned: Database update error for userId {userId}", ex);
                return false;
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"IsUserBanned: Invalid operation for userId {userId}", ex);
                return false;
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"IsUserBanned: SQL Server error for userId {userId}", ex);
                return false;
            }
            catch (Exception)
            {
                loggerHelper.LogInfo($"IsUserBanned: Error checking ban status for userId {userId}");
                return false;
            }
        }

        public int GetRemainingStrikes(int userId)
        {
            try
            {
                using (var context = contextFactory())
                {
                    var user = context.UserAccount.FirstOrDefault(u => u.idUser == userId);
                    if (user == null)
                    {
                        return 0;
                    }

                    int activeCount = GetActiveStrikeCount(context, user);
                    return Math.Max(0, StrikeLimit - activeCount);
                }
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"GetRemainingStrikes: Invalid operation for userId {userId}", ex);
                return 0;
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"GetRemainingStrikes: SQL Server error for userId {userId}", ex);
                return 0;
            }
            catch (EntityException ex)
            {
                loggerHelper.LogError($"GetRemainingStrikes: Entity Framework connection error for userId {userId}", ex);
                return 0;
            }
            catch (Exception)
            {
                loggerHelper.LogInfo($"GetRemainingStrikes: Error for userId {userId}");
                return 0;
            }
        }

        public List<StrikeInfo> GetUserStrikeHistory(int userId)
        {
            try
            {
                using (var context = contextFactory())
                {
                    var now = DateTime.UtcNow;

                    var rawStrikes = context.UserHasStrike
                        .Where(uhs => uhs.idUser == userId)
                        .Join(context.Strike,
                              uhs => uhs.idStrike,
                              s => s.idStrike,
                              (uhs, s) => new { uhs, s })
                        .Join(context.StrikeKind,
                              combined => combined.s.idStrikeKind,
                              sk => sk.idStrikeKind,
                              (combined, sk) => new
                              {
                                  StrikeId = combined.s.idStrike,
                                  StrikeType = sk.name,
                                  StrikeDate = combined.uhs.strikeDate,
                                  ExpirationDate = combined.s.endDate
                              })
                        .ToList();

                    var strikes = new List<StrikeInfo>();

                    foreach (var s in rawStrikes)
                    {
                        strikes.Add(new StrikeInfo
                        {
                            StrikeId = s.StrikeId,
                            StrikeType = s.StrikeType,
                            StrikeDate = s.StrikeDate.HasValue ? s.StrikeDate.Value : DateTime.MinValue,
                            ExpirationDate = s.ExpirationDate,
                            IsActive = s.ExpirationDate > now
                        });
                    }

                    return strikes.OrderByDescending(s => s.StrikeDate).ToList();
                }
            }
            catch (ArgumentNullException ex)
            {
                loggerHelper.LogError($"GetUserStrikeHistory: Null argument for userId {userId}", ex);
                return new List<StrikeInfo>();
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"GetUserStrikeHistory: Invalid operation for userId {userId}", ex);
                return new List<StrikeInfo>();
            }
            catch (DbUpdateException ex)
            {
                loggerHelper.LogError($"GetUserStrikeHistory: Database update error for userId {userId}", ex);
                return new List<StrikeInfo>();
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"GetUserStrikeHistory: Error for userId {userId}", ex);
                return new List<StrikeInfo>();
            }
        }

        public int GetUserStrikes(int userId)
        {
            try
            {
                using (var context = contextFactory())
                {
                    var now = DateTime.UtcNow;

                    return context.UserHasStrike
                        .Where(uhs => uhs.idUser == userId)
                        .Join(context.Strike,
                              uhs => uhs.idStrike,
                              s => s.idStrike,
                              (uhs, s) => s)
                        .Count(s => s.endDate > now);
                }
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"SQL Server error getting strikes for user {userId}", ex);
                return 0;
            }
            catch (DbUpdateException ex)
            {
                loggerHelper.LogError($"EF Core database update error getting strikes for user {userId}", ex);
                return 0;
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Unexpected error getting strikes for user {userId}", ex);
                return 0;
            }
        }

        private bool HandleProfanityCheck(IDbContext context, UserAccount user, string message)
        {
            if (!profanityFilter.ContainsProfanity(message, out var badWords))
            {
                return true;
            }

            try
            {
                var reason = $"Used prohibited words: {string.Join(", ", badWords)}";
                AddStrikeByType(context, user, "Offensive Language", reason);

                int activeCount = GetActiveStrikeCount(context, user);
                if (activeCount >= StrikeLimit)
                {
                    BanUser(context, user);
                    loggerHelper.LogInfo($"User {user.username} reached {activeCount} strikes and was banned");
                }
                else
                {
                    loggerHelper.LogInfo($"User {user.username} received strike ({activeCount}/{StrikeLimit}). Words: {string.Join(", ", badWords)}");
                }

                return false;
            }
            catch (DbUpdateConcurrencyException)
            {
                loggerHelper.LogWarning($"HandleProfanityCheck: Concurrency conflict for user {user.username}");
                return false;
            }
            catch (DbUpdateException ex)
            {
                loggerHelper.LogError($"HandleProfanityCheck: Database update error for user {user.username}", ex);
                return false;
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"HandleProfanityCheck: Invalid operation for user {user.username}", ex);
                return false;
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"HandleProfanityCheck: SQL Server error for user {user.username}", ex);
                return false;
            }
            catch (Exception)
            {
                loggerHelper.LogInfo($"HandleProfanityCheck: Error processing strike for user {user.username}");
                return false;
            }
        }

        private bool IsSpamming(int userId)
        {
            try
            {
                var now = DateTime.UtcNow;

                var timestamps = userMessageTimestamps.GetOrAdd(userId, _ => new List<DateTime>());

                lock (timestamps)
                {
                    timestamps.RemoveAll(t => (now - t).TotalSeconds > SpamTimeWindowSeconds);

                    timestamps.Add(now);

                    return timestamps.Count > SpamMessageThreshold;
                }
            }
            catch (ArgumentNullException ex)
            {
                loggerHelper.LogError($"IsSpamming: Null argument for userId {userId}", ex);
                return false;
            }
            catch (ArgumentException ex)
            {
                loggerHelper.LogError($"IsSpamming: Invalid argument for userId {userId}", ex);
                return false;
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"IsSpamming: Invalid operation for userId {userId}", ex);
                return false;
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"IsSpamming: Error checking spam for userId {userId}", ex);
                return false;
            }
        }

        private bool AddStrikeByType(IDbContext context, UserAccount user, string strikeTypeName, string reason)
        {
            try
            {
                var strikeKind = context.StrikeKind.FirstOrDefault(sk => sk.name == strikeTypeName);

                if (strikeKind == null)
                {
                    loggerHelper.LogInfo($"StrikeKind '{strikeTypeName}' not found in database. Cannot add strike.");
                    return false;
                }

                var strike = new Strike
                {
                    startDate = DateTime.UtcNow,
                    endDate = DateTime.UtcNow.AddDays(StrikeExpirationDays),
                    idStrikeKind = strikeKind.idStrikeKind
                };

                context.Strike.Add(strike);
                context.SaveChanges();

                var userHasStrike = new UserHasStrike
                {
                    idUser = user.idUser,
                    idStrike = strike.idStrike,
                    strikeDate = DateTime.UtcNow
                };

                context.UserHasStrike.Add(userHasStrike);
                context.SaveChanges();

                loggerHelper.LogInfo($"Strike added for user {user.username}: {strikeTypeName} - {reason}");
                return true;
            }
            catch (DbUpdateException ex)
            {
                loggerHelper.LogError($"AddStrikeByType: Database update error for user {user.username}", ex);
                return false;
            }
            catch (Exception)
            {
                loggerHelper.LogInfo($"AddStrikeByType: Error for user {user.username}");
                return false;
            }
        }

        private int GetActiveStrikeCount(IDbContext context, UserAccount user)
        {
            try
            {
                var now = DateTime.UtcNow;

                return context.UserHasStrike
                    .Where(uhs => uhs.idUser == user.idUser)
                    .Join(context.Strike,
                          uhs => uhs.idStrike,
                          s => s.idStrike,
                          (uhs, s) => s)
                    .Count(s => s.endDate > now);
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError($"GetActiveStrikeCount: Invalid operation for user {user.username}", ex);
                return 0;
            }
            catch (SqlException ex)
            {
                loggerHelper.LogError($"GetActiveStrikeCount: SQL Server error for user {user.username}", ex);
                return 0;
            }
            catch (Exception)
            {
                loggerHelper.LogInfo($"GetActiveStrikeCount: Error counting strikes for user {user.username}");
                return 0;
            }
        }

        private void BanUser(IDbContext context, UserAccount user)
        {
            try
            {
                user.isBanned = true;
                context.SaveChanges();
                loggerHelper.LogInfo($"User {user.username} (ID: {user.idUser}) has been banned");
            }
            catch (DbUpdateException ex)
            {
                loggerHelper.LogError($"BanUser: Failed to ban user {user.username}", ex);
                throw;
            }
        }
    }
}
