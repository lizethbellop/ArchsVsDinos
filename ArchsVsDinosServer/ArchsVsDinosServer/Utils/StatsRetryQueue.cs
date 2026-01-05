using Contracts.DTO.Statistics;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Utils
{
    public static class StatsRetryQueue
    {

        private class RetryItem
        {
            public MatchResultDTO Data { get; set; }
            public int Attempts { get; set; }
            public DateTime FirstAttempt { get; set; }
        }

        private static readonly ConcurrentQueue<RetryItem> pendingStats = new ConcurrentQueue<RetryItem>();
        private static readonly Timer retryTimer;
        private static bool isRunning = false;

        private const int MaxRetries = 5; 
        private static Func<MatchResultDTO, bool> saveAction;

        static StatsRetryQueue()
        {
            retryTimer = new Timer(ProcessQueue, null, 60000, 60000);
        }

        public static void Configure(Func<MatchResultDTO, bool> action)
        {
            saveAction = action;
        }

        public static void Enqueue(MatchResultDTO stats)
        {
            var item = new RetryItem
            {
                Data = stats,
                Attempts = 0,
                FirstAttempt = DateTime.Now
            };

            pendingStats.Enqueue(item);
            Console.WriteLine($"[STATS RETRY] Match {stats.MatchId} queued for retry. Pending: {pendingStats.Count}");
        }

        private static void ProcessQueue(object state)
        {
            if (isRunning || pendingStats.IsEmpty || saveAction == null) return;

            isRunning = true;
            try
            {
                int processedCount = 0;
                int initialCount = pendingStats.Count;

                for (int i = 0; i < initialCount; i++)
                {
                    if (pendingStats.TryDequeue(out var item))
                    {
                        if (item.Attempts >= MaxRetries)
                        {
                            Console.WriteLine($"[STATS DEAD LETTER] Match {item.Data.MatchId} discarded after {item.Attempts} failed attempts.");
                            continue; 
                        }

                        bool saved = false;
                        try
                        {
                            saved = saveAction(item.Data);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[STATS RETRY] Error saving match {item.Data.MatchId}: {ex.Message}");
                        }

                        if (!saved)
                        {
                            item.Attempts++;
                            pendingStats.Enqueue(item);
                        }
                        else
                        {
                            processedCount++;
                            Console.WriteLine($"[STATS RETRY] Match {item.Data.MatchId} successfully recovered and saved.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[STATS RETRY] Retry cycle critical error: {ex.Message}");
            }
            finally
            {
                isRunning = false;
            }
        }
    }
}