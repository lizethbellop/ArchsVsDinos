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

        #region Public Methods

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

                    if (!ProcessUserBanStatus(context, user))
                    {
                        return false;
                    }

                    if (DetectAndHandleSpam(context, user, userId))
                    {
                        return false;
                    }

                    return HandleProfanityCheck(context, user, message);
                }
            }
            catch (Exception ex)
            {
                return HandleDatabaseError("CanSendMessage", userId, ex);
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
                        CheckAndApplyBan(context, user);
                        loggerHelper.LogInfo($"Manual strike added to {user.username}: {strikeTypeName} - {reason}");
                    }

                    return success;
                }
            }
            catch (Exception ex)
            {
                return HandleDatabaseError("AddManualStrike", userId, ex);
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

                    return SyncUserBanStatus(context, user);
                }
            }
            catch (Exception ex)
            {
                return HandleDatabaseError("IsUserBanned", userId, ex);
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
            catch (Exception ex)
            {
                HandleDatabaseError("GetRemainingStrikes", userId, ex);
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
                    var rawStrikes = FetchUserStrikes(context, userId);
                    return MapToStrikeInfoList(rawStrikes, now);
                }
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"GetUserStrikeHistory: Error for userId {userId}", ex);
                return new List<StrikeInfo>();
            }
        }

        public BanResult ProcessStrike(int userId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return new BanResult { CanSendMessage = true };
            }

            try
            {
                using (var context = contextFactory())
                {
                    var user = context.UserAccount.FirstOrDefault(u => u.idUser == userId);
                    if (user == null)
                    {
                        loggerHelper.LogWarning($"ProcessStrike: User {userId} not found");
                        return new BanResult { CanSendMessage = false };
                    }

                    return ProcessUserStrike(context, user, userId, message);
                }
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"ProcessStrike: Error for userId {userId}", ex);
                return new BanResult { CanSendMessage = false };
            }
        }

        #endregion

        #region Ban Status Management

        private bool ProcessUserBanStatus(IDbContext context, UserAccount user)
        {
            int activeCount = GetActiveStrikeCount(context, user);

            if (activeCount >= StrikeLimit)
            {
                ApplyBanIfNeeded(context, user, activeCount);
                return false;
            }

            RemoveBanIfNeeded(context, user);
            return true;
        }

        private void ApplyBanIfNeeded(IDbContext context, UserAccount user, int activeCount)
        {
            loggerHelper.LogInfo($"User {user.username} is banned ({activeCount} active strikes)");
            if (!user.isBanned)
            {
                user.isBanned = true;
                context.SaveChanges();
            }
        }

        private void RemoveBanIfNeeded(IDbContext context, UserAccount user)
        {
            if (user.isBanned)
            {
                user.isBanned = false;
                context.SaveChanges();
                loggerHelper.LogInfo($"User {user.username} unbanned (strikes expired)");
            }
        }

        private bool SyncUserBanStatus(IDbContext context, UserAccount user)
        {
            int activeCount = GetActiveStrikeCount(context, user);
            bool shouldBeBanned = activeCount >= StrikeLimit;

            if (user.isBanned != shouldBeBanned)
            {
                user.isBanned = shouldBeBanned;
                context.SaveChanges();
                loggerHelper.LogInfo($"Updated ban status for user {user.username}: {shouldBeBanned}");
            }

            return shouldBeBanned;
        }

        private void CheckAndApplyBan(IDbContext context, UserAccount user)
        {
            int activeCount = GetActiveStrikeCount(context, user);
            if (activeCount >= StrikeLimit)
            {
                BanUser(context, user);
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

        #endregion

        #region Strike Processing

        private BanResult ProcessUserStrike(IDbContext context, UserAccount user, int userId, string message)
        {
            int activeCount = GetActiveStrikeCount(context, user);

            if (activeCount >= StrikeLimit)
            {
                return HandleAlreadyBanned(context, user, activeCount);
            }

            UnbanIfNeeded(context, user, activeCount);

            if (IsSpamming(userId))
            {
                return HandleSpamStrike(context, user, userId);
            }

            if (profanityFilter.ContainsProfanity(message, out var badWords))
            {
                return HandleProfanityStrike(context, user, badWords);
            }

            return new BanResult { CanSendMessage = true };
        }

        private BanResult HandleAlreadyBanned(IDbContext context, UserAccount user, int activeCount)
        {
            if (!user.isBanned)
            {
                BanUser(context, user);
            }

            return new BanResult
            {
                CanSendMessage = false,
                CurrentStrikes = activeCount,
                ShouldBan = true
            };
        }

        private void UnbanIfNeeded(IDbContext context, UserAccount user, int activeCount)
        {
            if (user.isBanned && activeCount < StrikeLimit)
            {
                user.isBanned = false;
                context.SaveChanges();
                loggerHelper.LogInfo($"User {user.username} unbanned (strikes expired)");
            }
        }

        private BanResult HandleSpamStrike(IDbContext context, UserAccount user, int userId)
        {
            AddStrikeByType(context, user, "Spam", "Sending messages too quickly");
            int activeCount = GetActiveStrikeCount(context, user);
            bool shouldBan = activeCount >= StrikeLimit;

            if (shouldBan)
            {
                BanUser(context, user);
            }

            return new BanResult
            {
                CanSendMessage = false,
                CurrentStrikes = activeCount,
                ShouldBan = shouldBan
            };
        }

        private BanResult HandleProfanityStrike(IDbContext context, UserAccount user, List<string> badWords)
        {
            var reason = $"Used prohibited words: {string.Join(", ", badWords)}";
            AddStrikeByType(context, user, "Offensive Language", reason);

            int activeCount = GetActiveStrikeCount(context, user);
            bool shouldBan = activeCount >= StrikeLimit;

            if (shouldBan)
            {
                BanUser(context, user);
            }

            loggerHelper.LogInfo($"User {user.username} strike ({activeCount}/{StrikeLimit}). Words: {string.Join(", ", badWords)}");

            return new BanResult
            {
                CanSendMessage = false,
                CurrentStrikes = activeCount,
                ShouldBan = shouldBan
            };
        }

        #endregion

        #region Profanity & Spam Detection

        private bool HandleProfanityCheck(IDbContext context, UserAccount user, string message)
        {
            if (!profanityFilter.ContainsProfanity(message, out var badWords))
            {
                return true;
            }

            try
            {
                ApplyProfanityStrike(context, user, badWords);
                return false;
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"HandleProfanityCheck: Error for user {user.username}", ex);
                return false;
            }
        }

        private void ApplyProfanityStrike(IDbContext context, UserAccount user, List<string> badWords)
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
        }

        private bool DetectAndHandleSpam(IDbContext context, UserAccount user, int userId)
        {
            if (!IsSpamming(userId))
            {
                return false;
            }

            AddStrikeByType(context, user, "Spam", "Sending messages too quickly");
            loggerHelper.LogWarning($"User {user.username} detected spamming");
            return true;
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
            catch (Exception ex)
            {
                loggerHelper.LogError($"IsSpamming: Error checking spam for userId {userId}", ex);
                return false;
            }
        }

        #endregion

        #region Strike Management

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

                CreateAndSaveStrike(context, user, strikeKind, reason);
                return true;
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"AddStrikeByType: Error for user {user.username}", ex);
                return false;
            }
        }

        private void CreateAndSaveStrike(IDbContext context, UserAccount user, StrikeKind strikeKind, string reason)
        {
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

            loggerHelper.LogInfo($"Strike added for user {user.username}: {strikeKind.name} - {reason}");
        }

        public int GetActiveStrikeCount(IDbContext context, UserAccount user)
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
            catch (Exception ex)
            {
                loggerHelper.LogError($"GetActiveStrikeCount: Error for user {user.username}", ex);
                return 0;
            }
        }

        #endregion

        #region Strike History

        private List<dynamic> FetchUserStrikes(IDbContext context, int userId)
        {
            return context.UserHasStrike
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
                .ToList<dynamic>();
        }

        private List<StrikeInfo> MapToStrikeInfoList(List<dynamic> rawStrikes, DateTime now)
        {
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

        #endregion

        #region Error Handling

        private bool HandleDatabaseError(string methodName, int userId, Exception ex)
        {
            string errorType = GetErrorType(ex);
            loggerHelper.LogError($"{methodName}: {errorType} for userId {userId}", ex);
            return false;
        }

        private string GetErrorType(Exception ex)
        {
            if (ex is ArgumentNullException)
            {
                return "Null argument";
            }
            if (ex is DbUpdateConcurrencyException)
            {
                return "Concurrency conflict";
            }
            if (ex is DbUpdateException)
            {
                return "Database update error";
            }
            if (ex is InvalidOperationException)
            {
                return "Invalid operation";
            }
            if (ex is SqlException)
            {
                return "SQL Server error";
            }
            if (ex is DataException)
            {
                return "Data access error";
            }
            if (ex is EntityException)
            {
                return "Entity Framework connection error";
            }
            if (ex is TimeoutException)
            {
                return "Timeout";
            }
            return $"Unexpected error - {ex.Message}";
        }

        #endregion
    }
}
