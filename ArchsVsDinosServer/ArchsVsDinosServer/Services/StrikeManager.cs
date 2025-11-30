using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Services.StrikeService;
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
    public class StrikeManager : IStrikeManager
    {
        private readonly ContextExecutor contextExecutor;
        private readonly UserFetcher userFetcher;
        private readonly StrikeRepository strikeRepo;
        private readonly BanService banService;
        private readonly ProfanityService profanityService;
        private readonly SpamService spamService;
        private readonly ILoggerHelper logger;

        private const int StrikeLimit = 3;

        public StrikeManager()
            : this(new StrikeServiceDependencies())
        {
        }

        public StrikeManager(StrikeServiceDependencies deps)
        {
            logger = deps.Logger;
            contextExecutor = new ContextExecutor(deps.ContextFactory, deps.Logger);
            userFetcher = new UserFetcher(deps.Logger);
            strikeRepo = new StrikeRepository(deps.Logger);
            banService = new BanService(deps.Logger);
            profanityService = new ProfanityService(deps.Logger, deps.ProfanityFilter);
            spamService = new SpamService(deps.Logger);
        }

        public bool CanSendMessage(int userId, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return true;

            try
            {
                return contextExecutor.Exec("CanSendMessage", userId, context =>
                {
                    var user = userFetcher.GetUser(context, userId, "CanSendMessage");
                    if (user == null) return false;

                    int activeStrikes = strikeRepo.GetActiveStrikes(context, userId);

                    banService.UnbanIfExpired(context, user, activeStrikes);
                    if (activeStrikes >= StrikeLimit)
                    {
                        banService.BanUser(context, user);
                        return false;
                    }

                    if (spamService.IsSpamming(userId))
                    {
                        var spamKind = strikeRepo.GetStrikeKind(context, "Spam");
                        if (spamKind != null)
                        {
                            strikeRepo.AddStrike(context, userId, spamKind, "Sending messages too quickly");
                            logger.LogWarning($"User {user.username} detected spamming");
                        }

                        activeStrikes = strikeRepo.GetActiveStrikes(context, userId);
                        if (activeStrikes >= StrikeLimit)
                            banService.BanUser(context, user);

                        return false;
                    }

                    if (profanityService.ContainsProfanity(message, out var badWords))
                    {
                        var offensiveKind = strikeRepo.GetStrikeKind(context, "Offensive Language");
                        if (offensiveKind != null)
                        {
                            string reason = $"Used prohibited words: {string.Join(", ", badWords)}";
                            strikeRepo.AddStrike(context, userId, offensiveKind, reason);
                        }

                        activeStrikes = strikeRepo.GetActiveStrikes(context, userId);
                        if (activeStrikes >= StrikeLimit)
                        {
                            banService.BanUser(context, user);
                            logger.LogInfo($"User {user.username} reached {activeStrikes} strikes and was banned");
                        }
                        else
                        {
                            logger.LogInfo($"User {user.username} received strike ({activeStrikes}/{StrikeLimit}). Words: {string.Join(", ", badWords)}");
                        }

                        return false;
                    }

                    return true;
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"CanSendMessage: Unexpected error for userId {userId}", ex);
                return false;
            }
        }

        public bool AddManualStrike(int userId, string strikeType, string reason)
        {
            try
            {
                return contextExecutor.Exec("AddManualStrike", userId, context =>
                {
                    var user = userFetcher.GetUser(context, userId, "AddManualStrike");
                    if (user == null) return false;

                    var kind = strikeRepo.GetStrikeKind(context, strikeType);
                    if (kind == null)
                    {
                        logger.LogWarning($"AddManualStrike: StrikeKind '{strikeType}' not found");
                        return false;
                    }

                    strikeRepo.AddStrike(context, userId, kind, reason);
                    logger.LogInfo($"Manual strike added to {user.username}: {strikeType} - {reason}");

                    int activeStrikes = strikeRepo.GetActiveStrikes(context, userId);
                    if (activeStrikes >= StrikeLimit)
                        banService.BanUser(context, user);

                    return true;
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"AddManualStrike: Unexpected error for userId {userId}", ex);
                return false;
            }
        }

        public bool IsUserBanned(int userId)
        {
            try
            {
                return contextExecutor.Exec("IsUserBanned", userId, context =>
                {
                    var user = userFetcher.GetUser(context, userId, "IsUserBanned");
                    if (user == null) return false;

                    int activeStrikes = strikeRepo.GetActiveStrikes(context, userId);
                    return banService.SyncBanStatus(context, user, activeStrikes);
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"IsUserBanned: Unexpected error for userId {userId}", ex);
                return false;
            }
        }

        public int GetRemainingStrikes(int userId)
        {
            try
            {
                return contextExecutor.Exec("GetRemainingStrikes", userId, context =>
                {
                    int activeStrikes = strikeRepo.GetActiveStrikes(context, userId);
                    return Math.Max(0, StrikeLimit - activeStrikes);
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"GetRemainingStrikes: Unexpected error for userId {userId}", ex);
                return 0;
            }
        }

        public List<StrikeInfo> GetUserStrikeHistory(int userId)
        {
            try
            {
                return contextExecutor.Exec("GetUserStrikeHistory", userId, context =>
                {
                    return strikeRepo.GetHistory(context, userId);
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"GetUserStrikeHistory: Unexpected error for userId {userId}", ex);
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
                return contextExecutor.Exec("ProcessStrike", userId, context =>
                {
                    var user = userFetcher.GetUser(context, userId, "ProcessStrike");
                    if (user == null)
                    {
                        logger.LogWarning($"ProcessStrike: User {userId} not found");
                        return new BanResult { CanSendMessage = false };
                    }

                    int activeStrikes = strikeRepo.GetActiveStrikes(context, userId);

                    if (activeStrikes >= StrikeLimit)
                    {
                        if (!user.isBanned)
                        {
                            banService.BanUser(context, user);
                        }
                        logger.LogInfo($"User {user.username} is banned ({activeStrikes} active strikes)");
                        return new BanResult
                        {
                            CanSendMessage = false,
                            CurrentStrikes = activeStrikes,
                            ShouldBan = true
                        };
                    }

                    banService.UnbanIfExpired(context, user, activeStrikes);

                    if (spamService.IsSpamming(userId))
                    {
                        var spamKind = strikeRepo.GetStrikeKind(context, "Spam");
                        if (spamKind != null)
                        {
                            strikeRepo.AddStrike(context, userId, spamKind, "Sending messages too quickly");
                            logger.LogWarning($"User {user.username} detected spamming");
                        }

                        activeStrikes = strikeRepo.GetActiveStrikes(context, userId);
                        bool shouldBan = activeStrikes >= StrikeLimit;

                        if (shouldBan)
                        {
                            banService.BanUser(context, user);
                        }

                        return new BanResult
                        {
                            CanSendMessage = false,
                            CurrentStrikes = activeStrikes,
                            ShouldBan = shouldBan
                        };
                    }

                    if (profanityService.ContainsProfanity(message, out var badWords))
                    {
                        var offensiveKind = strikeRepo.GetStrikeKind(context, "Offensive Language");
                        if (offensiveKind != null)
                        {
                            string reason = $"Used prohibited words: {string.Join(", ", badWords)}";
                            strikeRepo.AddStrike(context, userId, offensiveKind, reason);
                        }

                        activeStrikes = strikeRepo.GetActiveStrikes(context, userId);
                        bool shouldBan = activeStrikes >= StrikeLimit;

                        if (shouldBan)
                        {
                            banService.BanUser(context, user);
                            logger.LogInfo($"User {user.username} reached {activeStrikes} strikes and was banned");
                        }
                        else
                        {
                            logger.LogInfo($"User {user.username} strike ({activeStrikes}/{StrikeLimit}). Words: {string.Join(", ", badWords)}");
                        }

                        return new BanResult
                        {
                            CanSendMessage = false,
                            CurrentStrikes = activeStrikes,
                            ShouldBan = shouldBan
                        };
                    }

                    return new BanResult { CanSendMessage = true };
                });
            }
            catch (Exception ex)
            {
                logger.LogError($"ProcessStrike: Unexpected error for userId {userId}", ex);
                return new BanResult { CanSendMessage = false };
            }
        }
    }
}
