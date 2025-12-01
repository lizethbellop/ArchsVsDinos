using ArchsVsDinosServer.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services.StrikeService
{
    public class SpamService
    {
        private readonly ILoggerHelper logger;
        private readonly ConcurrentDictionary<int, List<DateTime>> userMessageTimestamps;
        private const int SpamMessageThreshold = 5;
        private const int SpamTimeWindowSeconds = 10;

        public SpamService(ILoggerHelper logger)
        {
            this.logger = logger;
            this.userMessageTimestamps = new ConcurrentDictionary<int, List<DateTime>>();
        }

        public bool IsSpamming(int userId)
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
                logger.LogError($"IsSpamming: Error checking spam for userId {userId}", ex);
                return false;
            }
        }
    }

}
