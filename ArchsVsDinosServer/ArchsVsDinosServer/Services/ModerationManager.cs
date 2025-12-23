using ArchsVsDinosServer.BusinessLogic.Moderation;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using ArchsVsDinosServer.Wrappers;
using Contracts;
using Contracts.DTO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    [ServiceBehavior(
    ConcurrencyMode = ConcurrencyMode.Multiple,
    InstanceContextMode = InstanceContextMode.PerCall
)]
    public class ModerationManager : IModerationManager
    {
        private readonly ModerationEngine moderationEngine;
        private readonly ILoggerHelper logger;

        public ModerationManager()
        {
            logger = new LoggerHelperWrapper();

            const string DataFolder = "Data";
            const string BannedWordsFile = "bannedWords.txt";
            string bannedWordsPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                DataFolder,
                BannedWordsFile
            );

            var profanityFilter = new ProfanityFilter(logger, bannedWordsPath);
            moderationEngine = new ModerationEngine(profanityFilter);
        }

        public ModerationResult ModerateMessage(ModerationRequestDTO request)
        {
            if (request == null)
            {
                throw new FaultException("Moderation request cannot be null");
            }

            try
            {
                return moderationEngine.Moderate(request);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning($"Invalid moderation request: {ex.Message}");
                throw new FaultException(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError("Unexpected moderation error", ex);
                throw new FaultException("Internal moderation error");
            }
        }
    }

}
