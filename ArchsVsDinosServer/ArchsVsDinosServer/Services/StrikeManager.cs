using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    public class StrikeManager
    {
        private readonly ProfanityFilter profanityFilter;
        private readonly ILoggerHelper loggerHelper;
        private const int StrikeLimit = 3;
        private const int StrikeExpirationDays = 30;
        private readonly Func<IDbContext> contextFactory;

        public StrikeManager(ServiceDependencies dependencies, ProfanityFilter profanityFilter)
        {
            loggerHelper = dependencies.loggerHelper;
            contextFactory = dependencies.contextFactory;
            this.profanityFilter = profanityFilter;
        }

        public StrikeManager() : this(new ServiceDependencies(), new ProfanityFilter())
        {
        }

        public bool CanSendMessage(int userId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return true;

            try
            {
                using (var context = contextFactory()) 
                {
                    var user = context.UserAccount.FirstOrDefault(u => u.idUser == userId);

                    if (user == null)
                    {
                        loggerHelper.LogWarning($"User with ID {userId} not found.");
                        return false;
                    }

                    if (user.isBanned)
                    {
                        loggerHelper.LogInfo($"User {user.username} is banned. Message blocked.");
                        return false;
                    }

                    return HandleProfanityCheck(context, user, message);
                }
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError("Invalid operation accessing the database.", ex);
                return false;
            }
            catch (System.Data.DataException ex)
            {
                loggerHelper.LogError("Database access error while processing strike.", ex);
                return false;
            }
            catch (Exception ex)
            {
                loggerHelper.LogError($"Unexpected error in CanSendMessage: {ex.Message}", ex);
                return false;
            }
        }


        private bool HandleProfanityCheck(IDbContext context, UserAccount user, string message)
        {
            if (!profanityFilter.ContainsProfanity(message, out var badWords))
                return true; // clean message

            AddStrike(context, user, badWords);

            int activeCount = GetActiveStrikeCount(user);
            if (activeCount >= StrikeLimit)
            {
                BanUser(context, user);
                loggerHelper.LogInfo($"User {user.username} reached {activeCount} strikes and was banned.");
            }
            else
            {
                loggerHelper.LogInfo($"User {user.username} received strike ({activeCount}/{StrikeLimit}).");
            }

            return false; // block message
        }

        private void AddStrike(IDbContext context, UserAccount user, List<string> badWords)
        {
            try
            {
                var strikeKind = context.StrikeKind.FirstOrDefault(sk => sk.name == "Offensive Language")
                                 ?? context.StrikeKind.FirstOrDefault();

                if (strikeKind == null)
                    throw new InvalidOperationException("No StrikeKind found in database.");

                var strike = new Strike
                {
                    startDate = DateTime.UtcNow,
                    endDate = DateTime.UtcNow.AddDays(StrikeExpirationDays),
                    idStrikeKind = strikeKind.idStrikeKind
                };

                user.Strike.Add(strike);
                context.Strike.Add(strike);
                context.SaveChanges();

                loggerHelper.LogInfo($"Strike added for user {user.username}. Words: {string.Join(", ", badWords)}");
            }
            catch (InvalidOperationException ex)
            {
                loggerHelper.LogError("Error finding or adding strike kind.", ex);
                throw;
            }
            catch (System.Data.DataException ex)
            {
                loggerHelper.LogError("Database error adding strike.", ex);
                throw;
            }
        }

        private int GetActiveStrikeCount(UserAccount user)
        {
            var now = DateTime.UtcNow;
            return user.Strike.Count(s => s.endDate > now);
        }

        private void BanUser(IDbContext context, UserAccount user)
        {
            user.isBanned = true;
            context.SaveChanges();
        }
    }
}
