using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.Model
{
    public class CardExchangeContext
    {
        public string MatchCode { get; set; }
        public PlayerSession PlayerA { get; set; }
        public PlayerSession PlayerB { get; set; }
        public CardInGame CardFromA { get; set; }
        public CardInGame CardFromB { get; set; }
    }

}
