using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Model
{
    public class AttachBodyPartContext
    {
        public GameSession Session { get; set; }
        public PlayerSession Player { get; set; }
        public DinoInstance Dino { get; set; }
        public CardInGame Card { get; set; }
    }

}
