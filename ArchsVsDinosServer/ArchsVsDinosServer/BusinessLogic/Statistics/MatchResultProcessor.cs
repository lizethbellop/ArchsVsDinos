using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts.DTO.Statistics;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.Statistics
{
    public class MatchResultProcessor
    {
        private readonly Func<IDbContext> contextFactory;
        private readonly ILoggerHelper logger;

        public MatchResultProcessor(ServiceDependencies dependencies)
        {
            contextFactory = dependencies.contextFactory;
            logger = dependencies.loggerHelper;
        }

        public bool ProcessMatchResults(MatchResultDTO matchResult)
        {
            using (var context = contextFactory())
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        var match = context.GeneralMatch.FirstOrDefault(m => m.matchCode == matchResult.MatchId);

                        if (match == null)
                        {
                            logger.LogInfo($"ProcessMatchResults: Match {matchResult.MatchId} not found in database");
                            return false;
                        }

                        match.date = matchResult.MatchDate;

                        foreach (var playerResult in matchResult.PlayerResults)
                        {
                            var participant = context.MatchParticipants
                                .FirstOrDefault(mp => mp.idGeneralMatch == match.idGeneralMatch &&
                                                     mp.idPlayer == playerResult.UserId);

                            if (participant != null)
                            {
                                participant.points = playerResult.Points;
                                participant.isWinner = playerResult.IsWinner;

                                participant.archaeologistsEliminated = playerResult.ArchaeologistsEliminated;
                                participant.supremeBossesEliminated = playerResult.SupremeBossesEliminated;
                            }
                            else
                            {
                                logger.LogWarning($"ProcessMatchResults: Participant not found for user {playerResult.UserId} in match {matchResult.MatchId}");
                            }
                        }

                        UpdatePlayerStatistics(context, matchResult);

                        context.SaveChanges();
                        transaction.Commit();

                        logger.LogInfo($"ProcessMatchResults: Successfully processed match {matchResult.MatchId}");
                        return true;
                    }
                    catch (InvalidOperationException ex)
                    {
                        transaction.Rollback();
                        logger.LogError($"ProcessMatchResults: Invalid operation in match {matchResult.MatchId} - {ex.Message}", ex);
                        return false;
                    }
                    catch (EntityException ex)
                    {
                        transaction.Rollback();
                        logger.LogError($"ProcessMatchResults: Entity Framework error in match {matchResult.MatchId} - {ex.Message}", ex);
                        return false;
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();
                        logger.LogError($"ProcessMatchResults: SQL error in match {matchResult.MatchId} - {ex.Message}", ex);
                        return false;
                    }
                    catch (ArgumentNullException ex)
                    {
                        transaction.Rollback();
                        logger.LogError($"ProcessMatchResults: Argument null in match {matchResult.MatchId} - {ex.Message}", ex);
                        return false;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        logger.LogError($"ProcessMatchResults: Transaction failed for match {matchResult.MatchId} - {ex.Message}", ex);
                        return false;
                    }
                }
            }
        }

        private void UpdatePlayerStatistics(IDbContext context, MatchResultDTO matchResult)
        {
            foreach (var playerResult in matchResult.PlayerResults)
            {
                var player = context.Player.FirstOrDefault(p => p.idPlayer == playerResult.UserId);

                if (player != null)
                {
                    player.totalMatches++;

                    if (playerResult.IsWinner)
                    {
                        player.totalWins++;
                    }
                    else
                    {
                        player.totalLosses++;
                    }

                    player.totalPoints += playerResult.Points;
                }
                else
                {
                    logger.LogWarning($"UpdatePlayerStatistics: Player {playerResult.UserId} not found in database");
                }
            }
        }

        public GameStatisticsDTO GetMatchStatistics(string matchCode)
        {
            using (var context = contextFactory())
            {
                try
                {
                    var match = context.GeneralMatch.FirstOrDefault(m => m.matchCode == matchCode);

                    if (match == null)
                    {
                        logger.LogWarning($"GetMatchStatistics: Match {matchCode} not found");
                        return new GameStatisticsDTO();
                    }

                    var participants = context.MatchParticipants
                        .Where(mp => mp.idGeneralMatch == match.idGeneralMatch && !mp.isDefeated)
                        .OrderByDescending(mp => mp.points)
                        .ThenByDescending(mp => mp.isWinner)
                        .ToList();

                    if (!participants.Any())
                    {
                        logger.LogWarning($"GetMatchStatistics: No participants found for match {matchCode}");
                        return new GameStatisticsDTO();
                    }

                    var playerStats = new List<PlayerMatchStatsDTO>();
                    int currentPosition = 1;
                    int previousPoints = participants.First().points;

                    for (int i = 0; i < participants.Count; i++)
                    {
                        var participant = participants[i];

                        if (participant.points != previousPoints)
                        {
                            currentPosition = i + 1;
                            previousPoints = participant.points;
                        }

                        playerStats.Add(new PlayerMatchStatsDTO
                        {
                            UserId = participant.idPlayer,
                            Username = GetUsername(context, participant.idPlayer),
                            Position = currentPosition,
                            Points = participant.points,
                            IsWinner = participant.isWinner,
                            ArchaeologistsEliminated = participant.archaeologistsEliminated,
                            SupremeBossesEliminated = participant.supremeBossesEliminated
                        });
                    }

                    return new GameStatisticsDTO
                    {
                        MatchCode = matchCode,
                        MatchDate = match.date,
                        PlayerStats = playerStats.ToArray()
                    };
                }
                catch (NullReferenceException ex)
                {
                    logger.LogError($"GetMatchStatistics: Null reference for match {matchCode} - {ex.Message}", ex);
                    return new GameStatisticsDTO();
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogError($"GetMatchStatistics: Invalid operation for match {matchCode} - {ex.Message}", ex);
                    return new GameStatisticsDTO();
                }
                catch (Exception ex)
                {
                    logger.LogError($"GetMatchStatistics: Error getting stats for match {matchCode} - {ex.Message}", ex);
                    return new GameStatisticsDTO();
                }
            }
        }

        private string GetUsername(IDbContext context, int playerId)
        {
            try
            {
                var player = context.Player.FirstOrDefault(p => p.idPlayer == playerId);
                if (player == null)
                {
                    return "Unknown";
                }

                var userAccount = player.UserAccount.FirstOrDefault();
                return userAccount?.username ?? "Unknown";
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning($"GetUsername: Multiple UserAccounts for player {playerId} - {ex.Message}");
                return "Unknown";
            }
            catch (Exception ex)
            {
                logger.LogWarning($"GetUsername: Error getting username for player {playerId} - {ex.Message}");
                return "Unknown";
            }
        }
    }
}
