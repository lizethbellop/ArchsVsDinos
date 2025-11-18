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

    /// <summary>
    /// Maneja la lógica de finalización del juego
    /// </summary>
    public class GameEndHandler
    {
        private const int GameDurationMinutes = 20;

        public bool ShouldGameEnd(GameSession session)
        {
            if (session == null)
            {
                return false;
            }

            // Verificar si se acabaron las cartas
            if (AreAllDrawPilesEmpty(session))
            {
                return true;
            }

            // Verificar si se acabó el tiempo
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

            // Determinar razón
            if (AreAllDrawPilesEmpty(session))
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

            // Determinar ganador (jugador con más puntos)
            var winner = session.Players.OrderByDescending(p => p.Points).FirstOrDefault();
            if (winner != null)
            {
                result.Winner = winner;
                result.WinnerPoints = winner.Points;
            }

            return result;
        }

        private bool AreAllDrawPilesEmpty(GameSession session)
        {
            foreach (var pile in session.DrawPiles)
            {
                if (pile.Count > 0)
                {
                    return false;
                }
            }
            return true;
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
