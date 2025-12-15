using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Model
{
    public class LobbyPlayer
    {
        public int UserId { get; }
        public string Nickname { get; }
        public bool IsReady { get; set; }

        public LobbyPlayer(int userId, string nickname)
        {
            UserId = userId;
            Nickname = nickname;
        }
    }

}
