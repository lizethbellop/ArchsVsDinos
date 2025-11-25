using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic
{
    public class BanResult
    {
        public bool CanSendMessage { get; set; }
        public int CurrentStrikes { get; set; }
        public bool ShouldBan { get; set; }
    }
}
