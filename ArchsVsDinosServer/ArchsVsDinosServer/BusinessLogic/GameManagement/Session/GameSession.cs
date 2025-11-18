using ArchsVsDinosServer.BusinessLogic.GameManagement.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement.Session
{
    public class GameSession
    {
        public readonly object SyncRoot = new object();
        private readonly List<PlayerSession> players = new List<PlayerSession>();
        private readonly List<List<string>> drawPiles = new List<List<string>>();
        private readonly List<string> discardPile = new List<string>();

        public int MatchId { get; private set; }
        public int CurrentTurn { get; private set; }
        public int TurnNumber { get; private set; }
        public bool IsStarted { get; private set; }
        public DateTime? StartTime { get; private set; }
        public bool HasDrawnThisTurn { get; private set; }
        public bool HasTakenMainAction { get; private set; }
        public int CardsPlayedThisTurn { get; private set; }
        public CentralBoard CentralBoard { get; private set; }

        public IReadOnlyList<PlayerSession> Players => players.AsReadOnly();
        public IReadOnlyList<List<string>> DrawPiles => drawPiles.AsReadOnly();
        public IReadOnlyList<string> DiscardPile => discardPile.AsReadOnly();

        public GameSession(int matchId, CentralBoard board)
        {
            MatchId = matchId;
            CentralBoard = board ?? new CentralBoard();
        }

        public void AddPlayer(PlayerSession player)
        {
            lock (SyncRoot)
            {
                if (player != null)
                {
                    players.Add(player);
                }
            }
        }

        public void SetDrawPiles(List<List<string>> piles)
        {
            lock (SyncRoot)
            {
                drawPiles.Clear();
                if (piles != null)
                {
                    foreach (var pile in piles)
                    {
                        drawPiles.Add(new List<string>(pile));
                    }
                }
            }
        }

        public List<string> DrawFromPile(int pileIndex, int count)
        {
            lock (SyncRoot)
            {
                if (pileIndex < 0 || pileIndex >= drawPiles.Count || count <= 0)
                {
                    return new List<string>();
                }

                var pile = drawPiles[pileIndex];
                var availableCount = pile.Count < count ? pile.Count : count;
                var drawn = pile.Take(availableCount).ToList();
                pile.RemoveRange(0, drawn.Count);

                return drawn;
            }
        }

        public void AddToDiscard(string cardId)
        {
            lock (SyncRoot)
            {
                if (!string.IsNullOrWhiteSpace(cardId))
                {
                    discardPile.Add(cardId);
                }
            }
        }

        public void AddToDiscard(List<string> cardIds)
        {
            lock (SyncRoot)
            {
                if (cardIds != null)
                {
                    discardPile.AddRange(cardIds.Where(id => !string.IsNullOrWhiteSpace(id)));
                }
            }
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
            {
                CardsPlayedThisTurn++;
            }
        }

        public void MarkCardDrawn()
        {
            lock (SyncRoot)
            {
                HasDrawnThisTurn = true;
            }
        }

        public void MarkMainActionTaken()
        {
            lock (SyncRoot)
            {
                HasTakenMainAction = true;
            }
        }

        public void MarkAsStarted()
        {
            lock (SyncRoot)
            {
                IsStarted = true;
            }
        }

        public int GetDrawPileCount(int pileIndex)
        {
            lock (SyncRoot)
            {
                if (pileIndex >= 0 && pileIndex < drawPiles.Count)
                {
                    return drawPiles[pileIndex].Count;
                }
                return 0;
            }
        }

    }
}
