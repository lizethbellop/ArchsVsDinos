using ArchsVsDinosServer.BusinessLogic.GameManagement;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Services.Interfaces;
using ArchsVsDinosServer.Utils;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Services.GameService
{
    public class GameExpulsionHandler
    {
        /*private readonly GameSessionManager sessionManager;
        private readonly GameActionHandler actionHandler;
        private readonly GameNotificationService notificationService;
        private readonly ServiceDependencies dependencies;
        private readonly ILoggerHelper logger;

        public GameExpulsionHandler(
            GameSessionManager sessionManager,
            GameActionHandler actionHandler,
            GameNotificationService notificationService,
            ServiceDependencies dependencies,
            ILoggerHelper logger)
        {
            this.sessionManager = sessionManager;
            this.actionHandler = actionHandler;
            this.notificationService = notificationService;
            this.dependencies = dependencies;
            this.logger = logger;
        }

        #region IGameNotifier Implementation

        public void NotifyPlayerExpelled(string matchCode, string username, string reason)
        {
            try
            {
                logger.LogWarning($"NotifyPlayerExpelled: {username} expelled from game {matchCode}. Reason: {reason}");

                int matchId = GetMatchIdByCode(matchCode);
                if (matchId == 0)
                {
                    logger.LogWarning($"NotifyPlayerExpelled: Match {matchCode} not found");
                    return;
                }

                var session = sessionManager.GetSession(matchId);
                if (session == null)
                {
                    logger.LogWarning($"NotifyPlayerExpelled: Session {matchId} not found");
                    return;
                }

                var player = session.Players.FirstOrDefault(p => p.Username == username);
                if (player == null)
                {
                    logger.LogWarning($"NotifyPlayerExpelled: Player {username} not found in session");
                    return;
                }

                ExecutePlayerExpulsion(session, player, matchCode, reason);
            }
            catch (Exception ex)
            {
                logger.LogError($"NotifyPlayerExpelled: Error expelling {username} from {matchCode}", ex);
            }
        }

        public void NotifyGameClosure(string matchCode, string reason)
        {
            try
            {
                logger.LogWarning($"NotifyGameClosure: Closing game {matchCode}. Reason: {reason}");

                int matchId = GetMatchIdByCode(matchCode);
                if (matchId == 0)
                {
                    logger.LogWarning($"NotifyGameClosure: Match {matchCode} not found");
                    return;
                }

                var session = sessionManager.GetSession(matchId);
                if (session == null)
                {
                    logger.LogWarning($"NotifyGameClosure: Session {matchId} not found");
                    return;
                }

                ExecuteGameClosure(session, matchId, matchCode, reason);
            }
            catch (Exception ex)
            {
                logger.LogError($"NotifyGameClosure: Error closing game {matchCode}", ex);
            }
        }

        #endregion

        #region Expulsion Logic

        private void ExecutePlayerExpulsion(GameSession session, PlayerSession player, string matchCode, string reason)
        {
            notificationService.NotifyPlayerExpelled(session, player, reason);

            bool removed = session.RemovePlayer(player.Username);
            if (!removed)
            {
                logger.LogWarning($"ExecutePlayerExpulsion: Failed to remove player {player.Username}");
                return;
            }

            RecordPlayerExpulsion(session.MatchId, player.UserId, reason);

            if (session.Players.Count < 2)
            {
                logger.LogWarning($"ExecutePlayerExpulsion: Insufficient players. Closing game.");
                NotifyGameClosure(matchCode, "Insufficient players to continue");
            }
            else
            {
                HandleTurnAfterExpulsion(session, player.UserId);
                logger.LogInfo($"ExecutePlayerExpulsion: Player {player.Username} expelled successfully. Remaining: {session.Players.Count}");
            }
        }

        private void ExecuteGameClosure(GameSession session, int matchId, string matchCode, string reason)
        {
            var closureResult = new GameEndResult
            {
                Reason = $"Game closed: {reason}",
                Winner = null,
                WinnerPoints = 0
            };

            notificationService.NotifyGameEnded(session, closureResult);

            UpdateMatchAsClosed(matchId, reason);

            sessionManager.RemoveSession(matchId);

            logger.LogInfo($"ExecuteGameClosure: Game {matchCode} closed successfully");
        }

        private void HandleTurnAfterExpulsion(GameSession session, int expelledUserId)
        {
            if (session.CurrentTurn == expelledUserId)
            {
                var nextPlayer = actionHandler.GetNextPlayer(session);
                if (nextPlayer != null)
                {
                    session.StartTurn(nextPlayer.UserId);
                    notificationService.NotifyTurnChanged(session, nextPlayer, new GameEndHandler());
                }
            }
        }

        #endregion

        #region Database Operations

        private int GetMatchIdByCode(string matchCode)
        {
            try
            {
                using (var context = dependencies.contextFactory())
                {
                    var match = context.GeneralMatch.FirstOrDefault(m => m.matchCode == matchCode);
                    return match?.idGeneralMatch ?? 0;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"GetMatchIdByCode: Error finding match {matchCode}", ex);
                return 0;
            }
        }

        private void UpdateMatchAsClosed(int matchId, string reason)
        {
            try
            {
                using (var context = dependencies.contextFactory())
                {
                    var match = context.GeneralMatch.FirstOrDefault(m => m.idGeneralMatch == matchId);
                    if (match != null)
                    {
                        match.date = DateTime.UtcNow;
                        context.SaveChanges();
                        logger.LogInfo($"UpdateMatchAsClosed: Match {matchId} marked as closed");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"UpdateMatchAsClosed: Error updating match {matchId}", ex);
            }
        }

        private void RecordPlayerExpulsion(int matchId, int userId, string reason)
        {
            try
            {
                using (var context = dependencies.contextFactory())
                {
                    var participant = context.MatchParticipants.FirstOrDefault(
                        mp => mp.idGeneralMatch == matchId && mp.idPlayer == userId
                    );

                    if (participant != null)
                    {
                        participant.wasExpelled = true;
                        participant.expulsionReason = reason;
                        participant.expulsionDate = DateTime.UtcNow;

                        context.SaveChanges();
                        logger.LogInfo($"RecordPlayerExpulsion: Recorded expulsion for player {userId} from match {matchId}");
                    }
                    else
                    {
                        logger.LogWarning($"RecordPlayerExpulsion: Participant not found for player {userId} in match {matchId}");
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                logger.LogError($"RecordPlayerExpulsion: Database error recording expulsion for player {userId}", ex);
            }
            catch (Exception ex)
            {
                logger.LogError($"RecordPlayerExpulsion: Error recording expulsion for player {userId}", ex);
            }
        }

        #endregion*/
    }
}
