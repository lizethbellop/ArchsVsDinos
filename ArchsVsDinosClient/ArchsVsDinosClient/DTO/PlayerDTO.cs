using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.DTO
{
    public class PlayerDTO
    {
        public int IdPlayer { get; set; }
        public string Facebook { get; set; }
        public string Instagram { get; set; }
        public string X { get; set; }
        public string Tiktok { get; set; }
        public int TotalWins { get; set; }
        public int TotalLosses { get; set; }
        public int TotalPoints { get; set; }
        public string ProfilePicture { get; set; }
    }
}
