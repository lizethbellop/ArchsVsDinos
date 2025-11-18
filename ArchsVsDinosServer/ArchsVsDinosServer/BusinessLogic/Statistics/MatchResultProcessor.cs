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
                        var match = context.GeneralMatch.FirstOrDefault(m => m.idGeneralMatch == matchResult.MatchId);

                        if (match == null)
                        {
                            logger.LogInfo($"ProcessMatchResults: Match {matchResult.MatchId} not found in database");
                            return false;
                        }

                        match.date = matchResult.MatchDate;

                        foreach (var playerResult in matchResult.PlayerResults)
                        {
                            var participant = context.MatchParticipants
                                .FirstOrDefault(mp => mp.idGeneralMatch == matchResult.MatchId &&
                                                     mp.idPlayer == playerResult.UserId);

                            if (participant != null)
                            {
                                participant.points = playerResult.Points;
                                participant.isWinner = playerResult.IsWinner;
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
    }
}
