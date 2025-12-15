using ArchsVsDinosServer.BusinessLogic.GameManagement.Board;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using ArchsVsDinosServer.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement
{
    public class GameSessionManager
    {
        private readonly ConcurrentDictionary<string, GameSession> activeSessions;

        private readonly ILoggerHelper logger;

        public GameSessionManager(ILoggerHelper logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");

            activeSessions = new ConcurrentDictionary<string, GameSession>();
            this.logger = logger;
        }

        public bool CreateSession(string matchCode)
        {
            if (string.IsNullOrWhiteSpace(matchCode))
            {
                return false;
            }

            var centralBoard = new CentralBoard();

            var session = new GameSession(matchCode, centralBoard, this.logger);

            return activeSessions.TryAdd(matchCode, session);
        }

        public GameSession GetSession(string matchCode)
        {
            activeSessions.TryGetValue(matchCode, out var session);
            return session;
        }

        public bool RemoveSession(string matchCode)
        {
            return activeSessions.TryRemove(matchCode, out _);
        }

        public bool SessionExists(string matchCode)
        {
            return activeSessions.ContainsKey(matchCode);
        }

        public PlayerSession GetPlayer(string matchCode, int userId)
        {
            var session = GetSession(matchCode);
            if (session == null)
            {
                return null;
            }

            return session.Players.FirstOrDefault(player => player.UserId == userId);
        }

        public bool IsPlayerInSession(string matchCode, int userId)
        {
            return GetPlayer(matchCode, userId) != null;
        }

        public List<GameSession> GetAllActiveSessions()
        {
            return new List<GameSession>(activeSessions.Values);
        }

        public int GetActiveSessionCount()
        {
            return activeSessions.Count;
        }

        
    }
}
