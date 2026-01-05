using Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement
{
    public class GameCallbackRegistry
    {
        private static GameCallbackRegistry instance;
        private static readonly object lockObject = new object();
        private readonly ConcurrentDictionary<int, IGameManagerCallback> playerCallbacks;
        private readonly ConcurrentDictionary<string, object> matchLocks = new ConcurrentDictionary<string, object>();

        private GameCallbackRegistry()
        {
            playerCallbacks = new ConcurrentDictionary<int, IGameManagerCallback>();
            matchLocks = new ConcurrentDictionary<string, object>();
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
            playerCallbacks[userId] = callback;
        }

        public IGameManagerCallback GetCallback(int userId)
        {
            playerCallbacks.TryGetValue(userId, out var callback);
            return callback;
        }

        public void UnregisterCallback(int userId)
        {
            IGameManagerCallback removedCallback;
            playerCallbacks.TryRemove(userId, out removedCallback);
        }

        public void UnregisterCallback(IGameManagerCallback callback)
        {
            if (callback == null) return;

            var entry = playerCallbacks.FirstOrDefault(pair => pair.Value == callback);
            if (!entry.Equals(default(KeyValuePair<int, IGameManagerCallback>)))
            {
                IGameManagerCallback removedCallback;
                playerCallbacks.TryRemove(entry.Key, out removedCallback);
            }
        }

        public object GetMatchLock(string matchCode)
        {
            return matchLocks.GetOrAdd(matchCode, key => new object());
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