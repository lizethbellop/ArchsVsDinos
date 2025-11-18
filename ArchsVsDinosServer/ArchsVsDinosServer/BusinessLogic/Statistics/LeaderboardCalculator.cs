using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts.DTO.Statistics;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.Statistics
{
    public class LeaderboardCalculator
    {
        private readonly Func<IDbContext> contextFactory;
        private readonly ILoggerHelper logger;

        public LeaderboardCalculator(ServiceDependencies dependencies)
        {
            contextFactory = dependencies.contextFactory;
            logger = dependencies.loggerHelper;
        }

        public List<LeaderboardEntryDTO> GetTopPlayers(int topN)
        {
            using (var context = contextFactory())
            {
                try
                {
                    var topPlayers = context.Player
                        .Where(p => p.totalPoints > 0)
                        .OrderByDescending(p => p.totalPoints)
                        .ThenByDescending(p => p.totalWins)
                        .Take(topN)
                        .ToList();

                    var leaderboard = new List<LeaderboardEntryDTO>();
                    int position = 1;

                    foreach (var player in topPlayers)
                    {
                        try
                        {
                            string username = player.UserAccount.Single().username;

                            leaderboard.Add(new LeaderboardEntryDTO
                            {
                                Position = position++,
                                UserId = player.idPlayer,
                                Username = username,
                                TotalPoints = player.totalPoints,
                                TotalWins = player.totalWins,
                            });
                        }
                        catch (InvalidOperationException ex)
                        {
                            logger.LogWarning($"GetTopPlayers: Player {player.idPlayer} has 0 or multiple UserAccounts - {ex.Message}");
                            leaderboard.Add(new LeaderboardEntryDTO
                            {
                                Position = position++,
                                UserId = player.idPlayer,
                                Username = "Unknown",
                                TotalPoints = player.totalPoints,
                                TotalWins = player.totalWins,
                            });
                        }
                    }

                    return leaderboard;
                }
                catch (ArgumentNullException ex)
                {
                    logger.LogError($"GetTopPlayers: Null argument - {ex.Message}", ex);
                    return new List<LeaderboardEntryDTO>();
                }
                catch (DbUpdateException ex)
                {
                    logger.LogError($"GetTopPlayers: Database error - {ex.Message}", ex);
                    return new List<LeaderboardEntryDTO>();
                }
                catch (Exception ex)
                {
                    logger.LogInfo($"GetTopPlayers: Error getting leaderboard - {ex.Message}");
                    return new List<LeaderboardEntryDTO>();
                }
            }
        }

        public int GetPlayerRank(int userId)
        {
            using (var context = contextFactory())
            {
                try
                {
                    var player = context.Player.FirstOrDefault(p => p.idPlayer == userId);

                    if (player == null)
                    {
                        return -1;
                    }

                    var rank = context.Player
                        .Where(p => p.totalPoints > player.totalPoints ||
                                   (p.totalPoints == player.totalPoints && p.totalWins > player.totalWins))
                        .Count() + 1;

                    return rank;
                }
                catch (ArgumentNullException ex)
                {
                    logger.LogError($"GetPlayerRank: Null argument - {ex.Message}", ex);
                    return -1;
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogError($"GetPlayerRank: Invalid operation - {ex.Message}", ex);
                    return -1;
                }
                catch (DbUpdateException ex)
                {
                    logger.LogError($"GetPlayerRank: Database error - {ex.Message}", ex);
                    return -1;
                }
                catch (Exception ex)
                {
                    logger.LogError($"GetPlayerRank: Error getting rank for user {userId} - {ex.Message}", ex);
                    return -1;
                }
            }
        }

        public List<LeaderboardEntryDTO> GetTopPlayersByWins(int topN)
        {
            using (var context = contextFactory())
            {
                try
                {
                    var topPlayers = context.Player
                        .Where(p => p.totalWins > 0)
                        .OrderByDescending(p => p.totalWins)
                        .ThenByDescending(p => p.totalPoints)
                        .Take(topN)
                        .ToList();

                    var leaderboard = new List<LeaderboardEntryDTO>();
                    int position = 1;

                    foreach (var player in topPlayers)
                    {
                        try
                        {
                            leaderboard.Add(new LeaderboardEntryDTO
                            {
                                Position = position++,
                                UserId = player.idPlayer,
                                Username = player.UserAccount.Single().username,
                                TotalPoints = player.totalPoints,
                                TotalWins = player.totalWins
                            });
                        }
                        catch (InvalidOperationException ex)
                        {
                            logger.LogWarning($"GetTopPlayersByWins: UserAccount issue for player {player.idPlayer} - {ex.Message}");
                            leaderboard.Add(new LeaderboardEntryDTO
                            {
                                Position = position++,
                                UserId = player.idPlayer,
                                Username = "Unknown",
                                TotalPoints = player.totalPoints,
                                TotalWins = player.totalWins
                            });
                        }
                    }

                    return leaderboard;
                }
                catch (ArgumentNullException ex)
                {
                    logger.LogError($"GetTopPlayersByWins: Null argument - {ex.Message}", ex);
                    return new List<LeaderboardEntryDTO>();
                }
                catch (DbUpdateException ex)
                {
                    logger.LogError($"GetTopPlayersByWins: Database error - {ex.Message}", ex);
                    return new List<LeaderboardEntryDTO>();
                }
                catch (Exception ex)
                {
                    logger.LogError($"GetTopPlayersByWins: Error - {ex.Message}", ex);
                    return new List<LeaderboardEntryDTO>();
                }
            }
        }
    }
}
