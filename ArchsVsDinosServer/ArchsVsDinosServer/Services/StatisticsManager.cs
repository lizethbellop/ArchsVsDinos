using ArchsVsDinosServer.BusinessLogic.Statistics;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts;
using Contracts.DTO.Result_Codes;
using Contracts.DTO.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class StatisticsManager : IStatisticsManager
    {
        private readonly MatchResultProcessor matchProcessor;
        private readonly LeaderboardCalculator leaderboardCalc;
        private readonly StatisticsHelper statsHelper;
        private readonly ServiceDependencies dependencies;
        private readonly ILoggerHelper logger;

        public StatisticsManager()
        {
            dependencies = new ServiceDependencies();
            matchProcessor = new MatchResultProcessor(dependencies);
            leaderboardCalc = new LeaderboardCalculator(dependencies);
            statsHelper = new StatisticsHelper(dependencies);
            logger = dependencies.loggerHelper;
            StatsRetryQueue.Configure(matchProcessor.ProcessMatchResults);
        }
        public SaveMatchResultCode SaveMatchStatistics(MatchResultDTO matchResult)
        {
            try
            {
                if (matchResult == null)
                {
                    return SaveMatchResultCode.InvalidData;
                }

                if (string.IsNullOrWhiteSpace(matchResult.MatchId))
                {
                    return SaveMatchResultCode.InvalidData;
                }

                if (matchResult.PlayerResults == null || !matchResult.PlayerResults.Any())
                {
                    return SaveMatchResultCode.InvalidData;
                }

                bool success = false;
                try
                {
                    success = matchProcessor.ProcessMatchResults(matchResult);
                }
                catch (Exception ex)
                {
                    logger.LogError($"SaveMatchStatistics: DB Error immediate: {ex.Message}", ex);
                    success = false;
                }

                if (success)
                {
                    logger.LogInfo($"SaveMatchStatistics: Successfully saved match {matchResult.MatchId}");
                    return SaveMatchResultCode.Success;
                }
                else
                {

                    logger.LogWarning($"SaveMatchStatistics: Failed to save match {matchResult.MatchId}. Queuing for background retry.");
                    StatsRetryQueue.Enqueue(matchResult);
                    return SaveMatchResultCode.DatabaseError;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"SaveMatchStatistics: Critical error - {ex.Message}", ex);
                StatsRetryQueue.Enqueue(matchResult);

                return SaveMatchResultCode.UnexpectedError;
            }
        }

        public PlayerStatisticsDTO GetPlayerStatistics(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    logger.LogWarning($"GetPlayerStatistics: Invalid userId {userId}");
                    return null;
                }

                var stats = statsHelper.GetPlayerStatistics(userId);

                if (stats == null)
                {
                    logger.LogWarning($"GetPlayerStatistics: Player {userId} not found");
                }

                return stats;
            }
            catch (Exception ex)
            {
                logger.LogError($"GetPlayerStatistics: Error getting stats for user {userId} - {ex.Message}", ex);
                return null;
            }
        }

        public List<LeaderboardEntryDTO> GetLeaderboard(int topN)
        {
            try
            {
                if (topN <= 0)
                {
                    topN = 10;
                }

                var leaderboard = leaderboardCalc.GetTopPlayers(topN);

                logger.LogInfo($"GetLeaderboard: Retrieved top {topN} players");
                return leaderboard;
            }
            catch (Exception ex)
            {
                logger.LogError($"GetLeaderboard: Error getting leaderboard - {ex.Message}", ex);
                return new List<LeaderboardEntryDTO>();
            }
        }

        public List<MatchHistoryDTO> GetPlayerMatchHistory(int userId, int count)
        {
            try
            {
                if (userId <= 0)
                {
                    logger.LogWarning($"GetPlayerMatchHistory: Invalid userId {userId}");
                    return new List<MatchHistoryDTO>();
                }

                if (count <= 0)
                {
                    count = 10;
                }

                var history = statsHelper.GetMatchHistory(userId, count);

                logger.LogInfo($"GetPlayerMatchHistory: Retrieved {history.Count} matches for user {userId}");
                return history;
            }
            catch (Exception ex)
            {
                logger.LogError($"GetPlayerMatchHistory: Error getting history for user {userId} - {ex.Message}", ex);
                return new List<MatchHistoryDTO>();
            }
        }

        public GameStatisticsDTO GetMatchStatistics(string matchCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(matchCode))
                {
                    logger.LogWarning($"GetMatchStatistics: Invalid matchCode {matchCode}");
                    return new GameStatisticsDTO();
                }

                var processor = new MatchResultProcessor(dependencies);
                return processor.GetMatchStatistics(matchCode);
            }
            catch (Exception ex)
            {
                logger.LogError($"GetMatchStatistics: Error getting stats for match {matchCode} - {ex.Message}", ex);
                return new GameStatisticsDTO();
            }
        }
    }
}
