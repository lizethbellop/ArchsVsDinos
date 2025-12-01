using ArchsVsDinosClient.GameService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.DTO
{
   public class PlayerInGame
    {
        public int UserId { get; set; }
        public List<CardDTO> Hand { get; set; } = new List<CardDTO>();
        public IGameManagerCallback Callback { get; set; }
    }
}
