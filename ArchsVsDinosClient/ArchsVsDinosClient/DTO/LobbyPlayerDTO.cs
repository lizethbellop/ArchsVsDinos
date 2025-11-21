using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.DTO
{
    public class LobbyPlayerDTO
    {
        public int IdPlayer { get; set; }
        public string Username { get; set; }
        public string Nickname { get; set; }
        public string ProfilePicture { get; set; }
        public int TotalWins { get; set; }
        public int TotalLosses { get; set; }

        public bool IsHost { get; set; }
    }
}
