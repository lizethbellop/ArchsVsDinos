using ArchsVsDinosClient.GameService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.DTO
{
    public class GameSession
    {
        public int MatchId { get; set; }
        public Dictionary<int, PlayerInGame> Players { get; set; } = new Dictionary<int, PlayerInGame>();
        public List<CardDTO> GeneralDeck { get; set; } = new List<CardDTO>();
        public int CurrentTurnPlayerId { get; set; }
    }
}
