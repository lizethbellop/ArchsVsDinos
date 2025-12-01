using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts.DTO.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.Infrastructure;

namespace ArchsVsDinosServer.BusinessLogic.Statistics
{
    public class StatisticsHelper
    {
        private readonly Func<IDbContext> contextFactory;
        private readonly ILoggerHelper logger;

        public StatisticsHelper(ServiceDependencies dependencies)
        {
            contextFactory = dependencies.contextFactory;
            logger = dependencies.loggerHelper;
        }

        public PlayerStatisticsDTO GetPlayerStatistics(int userId)
        {
            using (var context = contextFactory())
            {
                try
                {
                    var player = context.Player.FirstOrDefault(p => p.idPlayer == userId);

                    if (player == null)
                    {
                        logger.LogWarning($"GetPlayerStatistics: Player {userId} not found");
                        return new PlayerStatisticsDTO();
                    }

                    var totalWins = player.totalWins;
                    var totalLosses = player.totalLosses;
                    var totalMatches = player.totalMatches;
                    var totalPoints = player.totalPoints;

                    double winRate = 0;
                    if (totalMatches > 0)
                    {
                        winRate = Math.Round((double)totalWins / totalMatches * 100, 2);
                    }

                    string username = "";

                    try
                    {
                        username = player.UserAccount.Single().username;
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.LogError($"GetPlayerStatistics: Multiple or no UserAccounts found for player {userId}", ex);
                        username = "Unknown";
                    }

                    return new PlayerStatisticsDTO
                    {
                        UserId = player.idPlayer,
                        Username = username,
                        TotalWins = totalWins,
                        TotalLosses = totalLosses,
                        TotalMatches = totalMatches,
                        TotalPoints = totalPoints,
                        WinRate = winRate
                    };
                }
                catch (NullReferenceException ex)
                {
                    logger.LogError($"GetPlayerStatistics: Null reference for user {userId} - {ex.Message}", ex);
                    return new PlayerStatisticsDTO();
                }
                catch (DbUpdateException ex)
                {
                    logger.LogError($"GetPlayerStatistics: Database update error for user {userId} - {ex.Message}", ex);
                    return new PlayerStatisticsDTO();
                }
                catch (Exception ex)
                {
                    logger.LogError($"GetPlayerStatistics: Error for user {userId} - {ex.Message}", ex);
                    return new PlayerStatisticsDTO();
                }
            }
        }

        public List<MatchHistoryDTO> GetMatchHistory(int userId, int count)
        {
            using (var context = contextFactory())
            {
                try
                {
                    var matches = context.MatchParticipants
                        .Where(mp => mp.idPlayer == userId)
                        .OrderByDescending(mp => mp.GeneralMatch.date)
                        .Take(count)
                        .ToList();

                    var history = new List<MatchHistoryDTO>();

                    foreach (var match in matches)
                    {
                        try
                        {
                            if (match.GeneralMatch == null)
                            {
                                logger.LogWarning($"GetMatchHistory: GeneralMatch is null for match participant {match.idMatchParticipant}");
                                continue;
                            }

                            var totalPlayers = context.MatchParticipants
                                .Count(mp => mp.idGeneralMatch == match.idGeneralMatch);

                            var position = context.MatchParticipants
                                .Where(mp => mp.idGeneralMatch == match.idGeneralMatch &&
                                             mp.points > match.points)
                                .Count() + 1;

                            history.Add(new MatchHistoryDTO
                            {
                                MatchId = match.idGeneralMatch,
                                MatchDate = match.GeneralMatch.date,
                                Points = match.points,
                                Won = match.isWinner,
                                Position = position,
                                TotalPlayers = totalPlayers
                            });
                        }
                        catch (NullReferenceException ex)
                        {
                            logger.LogError($"GetMatchHistory: Null reference in match participant {match.idMatchParticipant} - {ex.Message}", ex);
                        }
                        catch (InvalidOperationException ex)
                        {
                            logger.LogError($"GetMatchHistory: Invalid operation in match participant {match.idMatchParticipant} - {ex.Message}", ex);
                        }
                    }

                    return history;
                }
                catch (DbUpdateException ex)
                {
                    logger.LogError($"GetMatchHistory: Database error for user {userId} - {ex.Message}", ex);
                    return new List<MatchHistoryDTO>();
                }
                catch (Exception ex)
                {
                    logger.LogError($"GetMatchHistory: Error for user {userId} - {ex.Message}", ex);
                    return new List<MatchHistoryDTO>();
                }
            }
        }

        public List<PlayerStatisticsDTO> GetMultiplePlayerStatistics(List<int> userIds)
        {
            using (var context = contextFactory())
            {
                var stats = new List<PlayerStatisticsDTO>();

                foreach (var userId in userIds)
                {
                    try
                    {
                        var playerStats = GetPlayerStatistics(userId);

                        // Verificar si el DTO está vacío (UserId == 0)
                        if (playerStats != null && playerStats.UserId != 0)
                        {
                            stats.Add(playerStats);
                        }
                        else
                        {
                            logger.LogInfo($"GetMultiplePlayerStatistics: Player stats not found for user {userId}");
                        }
                    }
                    catch (NullReferenceException ex)
                    {
                        logger.LogError($"GetMultiplePlayerStatistics: Null reference for user {userId} - {ex.Message}", ex);
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.LogError($"GetMultiplePlayerStatistics: Invalid operation for user {userId} - {ex.Message}", ex);
                    }
                    catch (Exception ex)
                    {
                        logger.LogInfo($"GetMultiplePlayerStatistics: Unexpected error for user {userId} - {ex.Message}");
                    }
                }

                return stats;
            }
        }

        public List<MatchHistoryDTO> GetRecentMatches(int count)
        {
            using (var context = contextFactory())
            {
                var history = new List<MatchHistoryDTO>();

                try
                {
                    var recentMatches = context.GeneralMatch
                        .Where(m => m.MatchParticipants.Any(mp => mp.isWinner))
                        .OrderByDescending(m => m.date)
                        .Take(count)
                        .ToList();

                    foreach (var match in recentMatches)
                    {
                        try
                        {
                            var totalPlayers = context.MatchParticipants
                                .Count(mp => mp.idGeneralMatch == match.idGeneralMatch);

                            var winnerParticipant = context.MatchParticipants
                                .FirstOrDefault(mp => mp.idGeneralMatch == match.idGeneralMatch && mp.isWinner);

                            if (winnerParticipant != null)
                            {
                                history.Add(new MatchHistoryDTO
                                {
                                    MatchId = match.idGeneralMatch,
                                    MatchDate = match.date,
                                    Points = winnerParticipant.points,
                                    Won = true,
                                    Position = 1,
                                    TotalPlayers = totalPlayers
                                });
                            }
                        }
                        catch (NullReferenceException ex)
                        {
                            logger.LogError($"GetRecentMatches: Null reference for match {match.idGeneralMatch} - {ex.Message}", ex);
                        }
                        catch (InvalidOperationException ex)
                        {
                            logger.LogError($"GetRecentMatches: Invalid operation for match {match.idGeneralMatch} - {ex.Message}", ex);
                        }
                        catch (Exception ex)
                        {
                            logger.LogInfo($"GetRecentMatches: Unexpected error for match {match.idGeneralMatch} - {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"GetRecentMatches: Error retrieving recent matches - {ex.Message}", ex);
                }

                return history;
            }
        }
    }
}
