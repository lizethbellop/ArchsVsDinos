using ArchsVsDinosServer.BusinessLogic.GameManagement.Board;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
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
        private static GameSessionManager instance;
        private static readonly object lockObject = new object();

        private readonly ConcurrentDictionary<int, GameSession> activeSessions;

        private GameSessionManager()
        {
            activeSessions = new ConcurrentDictionary<int, GameSession>();
        }

        public static GameSessionManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new GameSessionManager();
                        }
                    }
                }
                return instance;
            }
        }

        public bool CreateSession(int matchId)
        {
            if (matchId <= 0)
            {
                return false;
            }

            var centralBoard = new CentralBoard();
            var session = new GameSession(matchId, centralBoard);
            return activeSessions.TryAdd(matchId, session);
        }

        public GameSession GetSession(int matchId)
        {
            activeSessions.TryGetValue(matchId, out var session);
            return session;
        }

        public bool RemoveSession(int matchId)
        {
            return activeSessions.TryRemove(matchId, out _);
        }

        public bool SessionExists(int matchId)
        {
            return activeSessions.ContainsKey(matchId);
        }

        public PlayerSession GetPlayer(int matchId, int userId)
        {
            var session = GetSession(matchId);
            if (session == null)
            {
                return null;
            }

            foreach (var player in session.Players)
            {
                if (player.UserId == userId)
                {
                    return player;
                }
            }

            return null;
        }

        public bool IsPlayerInSession(int matchId, int userId)
        {
            return GetPlayer(matchId, userId) != null;
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
