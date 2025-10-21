using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.DTO
{
    public class PlayerDTO
    {
        public int idPlayer { get; set; }
        public string facebook { get; set; }
        public string instagram { get; set; }
        public string x { get; set; }
        public string tiktok { get; set; }
        public int totalWins { get; set; }
        public int totalLosses { get; set; }
        public int totalPoints { get; set; }
        public string profilePicture { get; set; }
    }
}
