using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Utils
{
    public class SessionManager
    {

        private static SessionManager sharedSessionManagerInstance;
        private static readonly object synchronizationLock = new object();

        private ConcurrentDictionary<string, bool> activeUsersDictionary;

        private SessionManager()
        {
            activeUsersDictionary = new ConcurrentDictionary<string, bool>();
        }

        public static SessionManager Instance
        {
            get
            {
                lock (synchronizationLock)
                {
                    if (sharedSessionManagerInstance == null)
                    {
                        sharedSessionManagerInstance = new SessionManager();
                    }
                    return sharedSessionManagerInstance;
                }
            }
        }

        public bool IsUserOnline(string username)
        {
            return activeUsersDictionary.ContainsKey(username);
        }

        public void RegisterUser(string username)
        {
            activeUsersDictionary.TryAdd(username, true);
        }

        public void RemoveUser(string username)
        {
            bool valueRemoved;
            activeUsersDictionary.TryRemove(username, out valueRemoved);
        }

        public void ClearAllUsers()
        {
            activeUsersDictionary.Clear();
        }

    }
}
