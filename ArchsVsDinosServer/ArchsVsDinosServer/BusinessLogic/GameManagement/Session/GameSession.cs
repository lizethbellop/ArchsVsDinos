using ArchsVsDinosServer.BusinessLogic.GameManagement.Board;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Model;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement.Session
{
    public class GameSession
    {
        public const int MaxMoves = 3;

        public readonly object SyncRoot = new object();
        private readonly ILoggerHelper loggerHelper;

        private readonly List<PlayerSession> players = new List<PlayerSession>();
        private readonly List<int> drawDeck = new List<int>();
        private readonly List<int> discardPile = new List<int>();
        private readonly Timer turnTimer;
        public event Action<string, int> TurnTimeExpired;

        public string MatchCode { get; private set; }
        public int CurrentTurn { get; private set; }
        public int TurnNumber { get; private set; }
        public bool IsStarted { get; private set; }
        public bool IsFinished { get; private set; }
        public DateTime? StartTime { get; private set; }
        public GameEndType? EndType { get; private set; }
        public DateTime MatchEndTime { get; private set; }
        public DateTime TurnEndTime { get; private set; }

        public int RemainingMoves { get; private set; }
        public CentralBoard CentralBoard { get; private set; }

        public IReadOnlyList<PlayerSession> Players => players.AsReadOnly();
        public IReadOnlyList<int> DrawDeck => drawDeck.AsReadOnly();
        public IReadOnlyList<int> DiscardPile => discardPile.AsReadOnly();

        private const int MatchDurationMinutes = 20;
        private const int TurnDurationSeconds = 30;

        public GameSession(string matchCode, CentralBoard board, ILoggerHelper logger)
        {
            MatchCode = matchCode ?? throw new ArgumentNullException(nameof(matchCode), "MatchCode cannot be null.");
            CentralBoard = board ?? throw new ArgumentNullException(nameof(board), "CentralBoard cannot be null.");
            loggerHelper = logger ?? throw new ArgumentNullException(nameof(logger), "Logger helper cannot be null.");

            CurrentTurn = 0;
            TurnNumber = 0;
            IsStarted = false;
            IsFinished = false;
            RemainingMoves = MaxMoves;
            turnTimer = new Timer(TurnDurationSeconds * 1000);
            turnTimer.AutoReset = false; 
            turnTimer.Elapsed += OnTurnTimerElapsed;
        }

        private void OnTurnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            TurnTimeExpired?.Invoke(MatchCode, CurrentTurn);
        }

        public void MarkAsStarted()
        {
            lock (SyncRoot)
            {
                IsStarted = true;
                StartTime = DateTime.UtcNow;
                MatchEndTime = StartTime.Value.AddMinutes(MatchDurationMinutes);
                ResetTurnTimer();
            }
        }

        public void ResetTurnTimer()
        {
            TurnEndTime = DateTime.UtcNow.AddSeconds(TurnDurationSeconds);
            turnTimer.Stop();
            turnTimer.Start();
        }

        public void MarkAsFinished(GameEndType endType)
        {
            lock (SyncRoot)
            {
                IsFinished = true;
                EndType = endType;
                turnTimer.Stop();
            }
        }


        public void AddPlayer(PlayerSession player)
        {
            lock (SyncRoot)
            {
                if (player != null) players.Add(player);
            }
        }

        public void SetDrawDeck(List<int> deck)
        {
            lock (SyncRoot)
            {
                drawDeck.Clear();
                if (deck != null)
                {
                    drawDeck.AddRange(deck);
                }
            }
        }

        public List<int> DrawCards(int count)
        {
            lock (SyncRoot)
            {
                if (count <= 0 || drawDeck.Count == 0)
                {
                    return new List<int>();
                }

                var availableCount = drawDeck.Count < count ? drawDeck.Count : count;
                var drawn = drawDeck.Take(availableCount).ToList();
                drawDeck.RemoveRange(0, drawn.Count);

                return drawn;
            }
        }

        public void AddToDiscard(int cardId)
        {
            lock (SyncRoot)
            {
                if (cardId > 0) discardPile.Add(cardId);
            }
        }

        public void AddToDiscard(List<int> cardIds)
        {
            lock (SyncRoot)
            {
                if (cardIds != null) discardPile.AddRange(cardIds.Where(id => id > 0));
            }
        }

        public bool RemoveFromDiscard(int cardId)
        {
            lock (SyncRoot)
            {
                if (discardPile.Contains(cardId))
                {
                    discardPile.Remove(cardId);
                    loggerHelper.LogInfo($"Card {cardId} removed from discard pile in match {MatchCode}");
                    return true;
                }
                return false;
            }
        }

        public bool RemovePlayer(string nickname)
        {
            lock (SyncRoot)
            {
                try
                {
                    var player = players.FirstOrDefault(playerSelected => playerSelected.Nickname == nickname);
                    if (player != null)
                    {
                        bool removed = players.Remove(player);
                        if (removed)
                        {
                            loggerHelper.LogInfo($"Player {nickname} removed from session {MatchCode}");
                        }
                        return removed;
                    }
                    return false;
                }
                catch (InvalidOperationException ex)
                {
                    loggerHelper.LogError($"Invalid operation removing player {nickname}", ex);
                    return false;
                }
                catch (Exception ex)
                {
                    loggerHelper.LogError($"Unexpected error removing player {nickname}", ex);
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
                    var player = players.FirstOrDefault(playerSelected => playerSelected.UserId == userId);
                    if (player != null)
                    {
                        bool removed = players.Remove(player);
                        return removed;
                    }
                    return false;
                }
                catch (InvalidOperationException ex)
                {
                    loggerHelper.LogError($"Invalid operation removing player {userId}", ex);
                    return false;
                }
                catch (Exception ex)
                {
                    loggerHelper.LogError($"Unexpected error removing player {userId}", ex);
                    return false;
                }
            }
        }

        public bool ConsumeMoves(int cost = 1)
        {
            lock (SyncRoot)
            {
                if (RemainingMoves >= cost)
                {
                    RemainingMoves -= cost;
                    return true;
                }
                return false;
            }
        }

        public void RestoreMoves(int cost = 1)
        {
            lock (SyncRoot)
            {
                RemainingMoves += cost;
                if (RemainingMoves > MaxMoves)
                {
                    RemainingMoves = MaxMoves;
                }
            }
        }

        public void StartTurn(int userId)
        {
            lock (SyncRoot)
            {
                CurrentTurn = userId;
                TurnNumber++;
                RemainingMoves = MaxMoves;
                loggerHelper.LogInfo($"Starting turn {TurnNumber} for user {userId} in match {MatchCode}.");
            }
        }

        public void EndTurn(int nextUserId)
        {
            lock (SyncRoot)
            {
                CurrentTurn = nextUserId;
                RemainingMoves = MaxMoves;
            }
        }
    }
}
