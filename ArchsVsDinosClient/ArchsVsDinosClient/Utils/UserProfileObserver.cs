using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Utils
{
    public class UserProfileObserver
    {
        private static UserProfileObserver _instance;

        public static UserProfileObserver Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UserProfileObserver();
                }
                return _instance;
            }
        }

        public event Action OnProfileUpdated;

        public void NotifyProfileUpdated()
        {
            OnProfileUpdated?.Invoke();
        }
    }
}
