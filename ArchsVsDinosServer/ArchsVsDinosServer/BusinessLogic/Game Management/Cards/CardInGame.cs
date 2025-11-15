using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.Game_Manager.Cards
{
    public class CardInGame
    {
        public string IdCardGlobal { get; set; } 
        public int? IdCardBody { get; set; }
        public int? IdCardCharacter { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string ArmyType { get; set; }
        public int Power { get; set; }
        public string ImagePath { get; set; }
    }
}
