using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services.StrikeService
{
    public class ProfanityService
    {
        private readonly ILoggerHelper logger;
        private readonly ProfanityFilter profanityFilter;

        public ProfanityService(ILoggerHelper logger, ProfanityFilter profanityFilter)
        {
            this.logger = logger;
            this.profanityFilter = profanityFilter;
        }

        public bool ContainsProfanity(string message, out List<string> badWords)
        {
            badWords = new List<string>();
            if (string.IsNullOrWhiteSpace(message)) return false;

            try
            {
                return profanityFilter.ContainsProfanity(message, out badWords);
            }
            catch (Exception ex)
            {
                logger.LogError($"ContainsProfanity: Unexpected error checking message '{message}'", ex);
                return false;
            }
        }
    }

}
