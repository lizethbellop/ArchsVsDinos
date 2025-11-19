using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic
{
    public class StrikeInfo
    {
        public int StrikeId { get; set; }
        public string StrikeType { get; set; }
        public DateTime StrikeDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public bool IsActive { get; set; }
    }
}
