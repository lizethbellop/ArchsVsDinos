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
                    var userAccount = context.UserAccount.FirstOrDefault(user => user.idUser == userId);
                    if (userAccount == null)
                    {
                        logger.LogWarning($"GetPlayerStatistics: UserAccount {userId} not found");
                        return new PlayerStatisticsDTO();
                    }

                    int realPlayerId = userAccount.idPlayer; 

                    var player = context.Player.FirstOrDefault(playerSelected => playerSelected.idPlayer == realPlayerId);

                    if (player == null)
                    {
                        logger.LogWarning($"GetPlayerStatistics: Player profile {realPlayerId} (User {userId}) not found");
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

                    return new PlayerStatisticsDTO
                    {
                        UserId = userId, 
                        Username = userAccount.username, 
                        TotalWins = totalWins,
                        TotalLosses = totalLosses,
                        TotalMatches = totalMatches,
                        TotalPoints = totalPoints,
                        WinRate = winRate
                    };
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
                    var userAccount = context.UserAccount.FirstOrDefault(user => user.idUser == userId);
                    if (userAccount == null)
                    {
                        return new List<MatchHistoryDTO>();
                    }
                    int realPlayerId = userAccount.idPlayer;

                    var matches = context.MatchParticipants
                        .Include("GeneralMatch")
                        .Where(mp => mp.idPlayer == realPlayerId) 
                        .OrderByDescending(mp => mp.GeneralMatch.date)
                        .Take(count)
                        .ToList();

                    var history = new List<MatchHistoryDTO>();

                    foreach (var match in matches)
                    {
                        try
                        {
                            if (match.GeneralMatch == null) continue;

                            var totalPlayers = context.MatchParticipants
                                .Count(matchParticipant => matchParticipant.idGeneralMatch == match.idGeneralMatch);

                            var position = context.MatchParticipants
                                .Where(matchParticipant => matchParticipant.idGeneralMatch == match.idGeneralMatch &&
                                             matchParticipant.points > match.points)
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
                        catch (Exception ex)
                        {
                            logger.LogError($"Error processing match {match.idGeneralMatch}: {ex.Message}", ex);
                        }
                    }

                    return history;
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
                    var playerStats = GetPlayerStatistics(userId);
                    if (playerStats != null && playerStats.UserId != 0) stats.Add(playerStats);
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
                        .Where(match => match.MatchParticipants.Any(matchParticipant => matchParticipant.isWinner))
                        .OrderByDescending(match => match.date)
                        .Take(count)
                        .ToList();

                    foreach (var match in recentMatches)
                    {
                        try
                        {
                            var totalPlayers = context.MatchParticipants.Count(matchParticipant => matchParticipant.idGeneralMatch == match.idGeneralMatch);
                            var winnerParticipant = context.MatchParticipants.FirstOrDefault(matchParticipant => matchParticipant.idGeneralMatch == match.idGeneralMatch && matchParticipant.isWinner);

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
                        catch (Exception) {}
                    }
                }
                catch (Exception ex) { logger.LogError("RecentMatches error", ex); }
                return history;
            }
        }
    }
}
