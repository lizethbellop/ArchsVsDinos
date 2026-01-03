using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using Contracts.DTO.Game_DTO;
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
        public List<PlayerScoreDTO> FinalScores { get; set; }
    }

    public class GameEndHandler
    {

        private const int GameDurationMinutes = 20;

        public bool ShouldGameEnd(GameSession session)
        {
            if (session == null) return false;
            return session.DrawDeck.Count == 0 || HasTimeExpired(session);
        }

        public GameEndResult EndGame(GameSession session)
        {
            if (session == null || !session.Players.Any())
            {
                return null;
            }

            var result = new GameEndResult { GameEnded = true };

            var orderedPlayers = session.Players
                .OrderByDescending(player => player.Points)
                .ToList();

            if (orderedPlayers.Count == 0)
            {
                result.Reason = "Aborted";
                result.FinalScores = new List<PlayerScoreDTO>();
                return result;
            }

            bool allZero = orderedPlayers.All(player => player.Points == 0);

            if (allZero)
            {
                result.Reason = "ArchsVictory";
                result.Winner = null;
                result.WinnerPoints = 0;
            }
            else
            {
                var winner = orderedPlayers.First();
                result.Reason = "DinosVictory";
                result.Winner = winner;
                result.WinnerPoints = winner.Points;
            }

            result.FinalScores = new List<PlayerScoreDTO>();
            int currentPosition = 1;

            foreach (var player in orderedPlayers)
            {
                result.FinalScores.Add(new PlayerScoreDTO
                {
                    UserId = player.UserId,
                    Username = player.Nickname,
                    Points = player.Points,
                    Position = currentPosition++ 
                });
            }

            return result;
        }

        private bool HasTimeExpired(GameSession session)
        {
            if (!session.StartTime.HasValue)
            {
                return false;
            }

            return (DateTime.UtcNow - session.StartTime.Value).TotalMinutes >= GameDurationMinutes;
        }

    }
}
