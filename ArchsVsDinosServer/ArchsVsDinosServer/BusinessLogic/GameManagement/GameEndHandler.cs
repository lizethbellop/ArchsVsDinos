using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement
{
    public class GameEndResult
    {
        public bool GameEnded { get; set; }
        public string Reason { get; set; }
        public PlayerSession Winner { get; set; }
        public int WinnerPoints { get; set; }
    }

    public class GameEndHandler
    {
        private const int GameDurationMinutes = 20;

        public bool ShouldGameEnd(GameSession session)
        {
            if (session == null)
            {
                return false;
            }

            if (IsDrawDeckEmpty(session))
            {
                return true;
            }

            if (HasTimeExpired(session))
            {
                return true;
            }

            return false;
        }

        public GameEndResult EndGame(GameSession session)
        {
            if (session == null || !session.Players.Any())
            {
                return null;
            }

            var result = new GameEndResult
            {
                GameEnded = true
            };

            if (IsDrawDeckEmpty(session))
            {
                result.Reason = "cards_depleted";
            }
            else if (HasTimeExpired(session))
            {
                result.Reason = "time_expired";
            }
            else
            {
                result.Reason = "unknown";
            }

            var winner = session.Players.OrderByDescending(p => p.Points).FirstOrDefault();
            if (winner != null)
            {
                result.Winner = winner;
                result.WinnerPoints = winner.Points;
            }

            return result;
        }

        private bool IsDrawDeckEmpty(GameSession session)
        {
            return session.DrawDeck.Count == 0;
        }

        private bool HasTimeExpired(GameSession session)
        {
            if (!session.StartTime.HasValue)
            {
                return false;
            }

            var elapsed = DateTime.UtcNow - session.StartTime.Value;
            return elapsed.TotalMinutes >= GameDurationMinutes;
        }

        public TimeSpan GetRemainingTime(GameSession session)
        {
            if (!session.StartTime.HasValue)
            {
                return TimeSpan.FromMinutes(GameDurationMinutes);
            }

            var elapsed = DateTime.UtcNow - session.StartTime.Value;
            var remaining = TimeSpan.FromMinutes(GameDurationMinutes) - elapsed;

            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }
}
