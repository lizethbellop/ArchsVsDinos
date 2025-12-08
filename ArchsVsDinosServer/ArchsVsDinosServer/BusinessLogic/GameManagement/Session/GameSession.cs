using ArchsVsDinosServer.BusinessLogic.GameManagement.Board;
using ArchsVsDinosServer.Interfaces;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement.Session
{
    public class GameSession
    {
        public readonly object SyncRoot = new object();
        private readonly List<PlayerSession> players = new List<PlayerSession>();
        private readonly List<List<int>> drawPiles = new List<List<int>>();
        private readonly List<int> discardPile = new List<int>();
        private readonly ILoggerHelper loggerHelper;

        public int MatchId { get; private set; }
        public int CurrentTurn { get; private set; }
        public int TurnNumber { get; private set; }
        public bool IsStarted { get; private set; }
        public DateTime? StartTime { get; private set; }
        public int CardsDrawnThisTurn { get; private set; }
        public bool HasTakenMainAction { get; private set; }
        public int CardsPlayedThisTurn { get; private set; }
        public int RemainingMoves { get; private set; }
        public CentralBoard CentralBoard { get; private set; }
        public IReadOnlyList<PlayerSession> Players => players.AsReadOnly();
        public IReadOnlyList<List<int>> DrawPiles => drawPiles.AsReadOnly();
        public IReadOnlyList<int> DiscardPile => discardPile.AsReadOnly();

        public GameSession(int matchId, CentralBoard board)
        {
            MatchId = matchId;
            CentralBoard = board ?? new CentralBoard();
            loggerHelper = new Wrappers.LoggerHelperWrapper();
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
        public void SetDrawPiles(List<List<int>> piles)
        {
            lock (SyncRoot)
            {
                drawPiles.Clear();
                if (piles != null)
                {
                    foreach (var pile in piles)
                    {
                        drawPiles.Add(new List<int>(pile));
                    }
                }
            }
        }

        public List<int> DrawFromPile(int pileIndex, int count)
        {
            lock (SyncRoot)
            {
                if (pileIndex < 0 || pileIndex >= drawPiles.Count || count <= 0)
                {
                    return new List<int>();
                }

                var pile = drawPiles[pileIndex];
                var availableCount = pile.Count < count ? pile.Count : count;
                var startIndex = pile.Count - availableCount;
                var drawn = pile.Skip(startIndex).Take(availableCount).ToList();
                pile.RemoveRange(startIndex, drawn.Count);

                return drawn;
            }
        }

        public void AddToDiscard(int cardId)
        {
            lock (SyncRoot)
            {
                if (cardId > 0)
                {
                    discardPile.Add(cardId);
                }
            }
        }

        public void AddToDiscard(List<int> cardIds)
        {
            lock (SyncRoot)
            {
                if (cardIds != null)
                {
                    discardPile.AddRange(cardIds.Where(id => id > 0));
                }
            }
        }

        public void StartTurn(int userId)
        {
            lock (SyncRoot)
            {
                CurrentTurn = userId;
                TurnNumber++;
                CardsDrawnThisTurn = 0;
                HasTakenMainAction = false;
                CardsPlayedThisTurn = 0;
                RemainingMoves = 3;
            }
        }

        public bool ConsumeMove()
        {
            lock (SyncRoot)
            {
                if (RemainingMoves > 0)
                {
                    RemainingMoves--;
                    return true;
                }
                return false;
            }
        }

        public void RestoreMove()
        {
            lock (SyncRoot)
            {
                if (RemainingMoves < 3)
                {
                    RemainingMoves++;
                }
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
                CardsDrawnThisTurn++;
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
                StartTime = DateTime.UtcNow;
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

        public bool RemovePlayer(string username)
        {
            lock (SyncRoot)
            {
                try
                {
                    var player = players.FirstOrDefault(p => p.Username == username);
                    if (player != null)
                    {
                        bool removed = players.Remove(player);
                        if (removed)
                        {
                            loggerHelper.LogInfo($"Player {username} removed from session {MatchId}");
                        }
                        return removed;
                    }
                    return false;
                }
                catch (InvalidOperationException ex)
                {
                    loggerHelper.LogError($"Invalid operation removing player {username}", ex);
                    return false;
                }
                catch (Exception ex)
                {
                    loggerHelper.LogError($"Unexpected error removing player {username}", ex);
                    return false;
                }
            }
        }

        public bool RemovePlayer(int userId)
        {
            lock (SyncRoot)
            {
                try
                {
                    var player = players.FirstOrDefault(p => p.UserId == userId);  
                    if (player != null)
                    {
                        bool removed = players.Remove(player);
                        return removed;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    loggerHelper.LogError($"Unexpected error removing player {userId}", ex);
                    return false;
                }
            }
        }
    }
}
