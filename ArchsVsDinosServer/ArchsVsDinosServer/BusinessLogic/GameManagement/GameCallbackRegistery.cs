using Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement
{
    public sealed class GameCallbackRegistry
    {
        private static GameCallbackRegistry instance;
        private static readonly object lockObject = new object();

        private readonly ConcurrentDictionary<int, IGameManagerCallback> playerCallbacks;
        private readonly ConcurrentDictionary<int, string> userMatchCodes;
        private readonly ConcurrentDictionary<string, object> matchLocks;

        private GameCallbackRegistry()
        {
            playerCallbacks = new ConcurrentDictionary<int, IGameManagerCallback>();
            userMatchCodes = new ConcurrentDictionary<int, string>();
            matchLocks = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public static GameCallbackRegistry Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        if (instance == null)
                        {
                            instance = new GameCallbackRegistry();
                        }
                    }
                }

                return instance;
            }
        }

        public void RegisterCallback(int userId, IGameManagerCallback callback)
        {
            if (userId <= 0)
            {
                return;
            }

            if (callback == null)
            {
                return;
            }

            playerCallbacks[userId] = callback;
        }

        public void RegisterCallback(int userId, string matchCode, IGameManagerCallback callback)
        {
            RegisterCallback(userId, callback);
            RegisterUserMatch(userId, matchCode);
        }

        public void RegisterUserMatch(int userId, string matchCode)
        {
            if (userId <= 0)
            {
                return;
            }

            string safeMatchCode = (matchCode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(safeMatchCode))
            {
                return;
            }

            userMatchCodes[userId] = safeMatchCode;
        }

        public bool TryGetMatchCode(int userId, out string matchCode)
        {
            matchCode = string.Empty;

            if (userId <= 0)
            {
                return false;
            }

            if (!userMatchCodes.TryGetValue(userId, out string stored))
            {
                return false;
            }

            matchCode = stored ?? string.Empty;
            return !string.IsNullOrWhiteSpace(matchCode);
        }

        public IGameManagerCallback GetCallback(int userId)
        {
            if (userId <= 0)
            {
                return null;
            }

            playerCallbacks.TryGetValue(userId, out IGameManagerCallback callback);
            return callback;
        }

        public void UnregisterCallback(int userId)
        {
            if (userId <= 0)
            {
                return;
            }

            playerCallbacks.TryRemove(userId, out IGameManagerCallback ignoredCallback);
        }

        public void UnregisterUserMatch(int userId)
        {
            if (userId <= 0)
            {
                return;
            }

            userMatchCodes.TryRemove(userId, out string ignoredMatchCode);
        }

        public void UnregisterPlayer(int userId)
        {
            UnregisterCallback(userId);
            UnregisterUserMatch(userId);
        }

        public void UnregisterCallback(IGameManagerCallback callback)
        {
            if (callback == null)
            {
                return;
            }

            var entry = playerCallbacks.FirstOrDefault(pair => pair.Value == callback);
            if (entry.Equals(default(KeyValuePair<int, IGameManagerCallback>)))
            {
                return;
            }

            playerCallbacks.TryRemove(entry.Key, out IGameManagerCallback ignoredCallback);
            userMatchCodes.TryRemove(entry.Key, out string ignoredMatchCode);
        }

        public object GetMatchLock(string matchCode)
        {
            string safeMatchCode = (matchCode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(safeMatchCode))
            {
                safeMatchCode = string.Empty;
            }

            return matchLocks.GetOrAdd(safeMatchCode, key => new object());
        }

        public KeyValuePair<int, IGameManagerCallback>[] GetRegisteredCallbacksSnapshot()
        {
            return playerCallbacks.ToArray();
        }

        public IEnumerable<IGameManagerCallback> GetAllCallbacks()
        {
            return playerCallbacks.Values;
        }
    }
}