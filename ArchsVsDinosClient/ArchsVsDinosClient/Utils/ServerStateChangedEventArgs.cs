using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Utils
{
    public class ServerStateChangedEventArgs : EventArgs
    {
        public bool IsAvailable { get; }
        public DateTime Timestamp { get; }

        public ServerStateChangedEventArgs(bool isAvailable)
        {
            IsAvailable = isAvailable;
            Timestamp = DateTime.Now;
        }
    }

}
