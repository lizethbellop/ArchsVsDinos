using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.Game_Manager.Session
{
    public class GameSession
    {
        public readonly object SyncRoot = new object();
        private readonly List<PlayerSession> players = new List<PlayerSession>();
        private readonly List<List<int>> drawPiles = new List<List<int>>();
        private readonly List<int> discardPile = new List<int>();

        public int MatchId { get; private set; }
        public int CurrentTurn { get; private set; }
        public int TurnNumber { get; private set; }
        public bool IsStarted { get; private set; }
        public bool HasDrawnThisTurn { get; private set; }
        public bool HasTakenMainAction { get; private set; }
        public int CardsPlayedThisTurn { get; private set; }
        public Board.CentralBoard CentralBoard { get; private set; }

        public IReadOnlyList<PlayerSession> Players => players.AsReadOnly();
        public IReadOnlyList<List<int>> DrawPiles => drawPiles.AsReadOnly();
        public IReadOnlyList<int> DiscardPile => discardPile.AsReadOnly();

        public GameSession(int matchId, Board.CentralBoard board)
        {
            MatchId = matchId;
            CentralBoard = board;
        }

        public void AddPlayer(PlayerSession player)
        {
            lock (SyncRoot)
                players.Add(player);
        }

        public void SetDrawPiles(List<List<int>> piles)
        {
            lock (SyncRoot)
            {
                drawPiles.Clear();
                foreach (var pile in piles)
                    drawPiles.Add(new List<int>(pile));
            }
        }

        public List<int> DrawFromPile(int pileIndex, int count)
        {
            lock (SyncRoot)
            {
                if (pileIndex < 0 || pileIndex >= drawPiles.Count)
                    return null;
                var pile = drawPiles[pileIndex];
                var drawn = pile.Take(count).ToList();
                pile.RemoveRange(0, drawn.Count);
                return drawn;
            }
        }

        public void AddToDiscard(int cardId)
        {
            lock (SyncRoot)
                discardPile.Add(cardId);
        }

        public void StartTurn(int userId)
        {
            lock (SyncRoot)
            {
                CurrentTurn = userId;
                TurnNumber++;
                HasDrawnThisTurn = false;
                HasTakenMainAction = false;
                CardsPlayedThisTurn = 0;
            }
        }

        public void MarkCardPlayed()
        {
            lock (SyncRoot)
                CardsPlayedThisTurn++;
        }

        public void MarkCardDrawn()
        {
            lock (SyncRoot)
                HasDrawnThisTurn = true;
        }

        public void MarkMainActionTaken()
        {
            lock (SyncRoot)
                HasTakenMainAction = true;
        }
    }
}
