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
                        var newMatch = new GeneralMatch
                        {
                            matchCode = matchResult.MatchId,
                            date = matchResult.MatchDate,
                            gameTime = DateTime.Now.TimeOfDay
                        };
                        context.GeneralMatch.Add(newMatch);
                        context.SaveChanges(); 

                        foreach (var playerResult in matchResult.PlayerResults)
                        {
                            if (playerResult.UserId <= 0) continue;

                            var userAccount = context.UserAccount.FirstOrDefault(u => u.idUser == playerResult.UserId);

                            if (userAccount == null)
                            {
                                continue;
                            }

                            int actualPlayerId = userAccount.idPlayer;

                            var participant = new MatchParticipants
                            {
                                idGeneralMatch = newMatch.idGeneralMatch,
                                idPlayer = actualPlayerId, 
                                points = playerResult.Points,
                                isWinner = playerResult.IsWinner,
                                isDefeated = !playerResult.IsWinner,
                                archaeologistsEliminated = playerResult.ArchaeologistsEliminated,
                                supremeBossesEliminated = playerResult.SupremeBossesEliminated
                            };
                            context.MatchParticipants.Add(participant);

                            var dbPlayer = context.Player.FirstOrDefault(player => player.idPlayer == actualPlayerId);
                            if (dbPlayer != null)
                            {
                                dbPlayer.totalMatches++;
                                dbPlayer.totalPoints += playerResult.Points;
                                if (playerResult.IsWinner) dbPlayer.totalWins++;
                                else dbPlayer.totalLosses++;
                            }
                        }
                        context.SaveChanges();
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        logger.LogError($"[STATS] Error saving results: {ex.Message}", ex);
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
                    var match = context.GeneralMatch.FirstOrDefault(matchSelected => matchSelected.matchCode == matchCode);

                    if (match == null)
                    {
                        logger.LogWarning($"GetMatchStatistics: Match {matchCode} not found");
                        return new GameStatisticsDTO();
                    }

                    var participants = context.MatchParticipants
                        .Where(matchParticipant => matchParticipant.idGeneralMatch == match.idGeneralMatch && !matchParticipant.isDefeated)
                        .OrderByDescending(matchParticipant => matchParticipant.points)
                        .ThenByDescending(matchParticipant => matchParticipant.isWinner)
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
                            SupremeBossesEliminated = participant.supremeBossesEliminated,
                            ProfilePicture = context.Player.FirstOrDefault(player => player.idPlayer == participant.idPlayer)?.profilePicture?? "/Resources/Images/Avatars/default_avatar_00.png"
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
                var player = context.Player.FirstOrDefault(playerSelected => playerSelected.idPlayer == playerId);
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
