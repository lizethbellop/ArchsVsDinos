using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.Game_Manager.Board
{
    public class CentralBoard
    {
        public List<int> LandArmy { get; set; } = new List<int>();
        public List<int> SeaArmy { get; set; } = new List<int>();
        public List<int> SkyArmy { get; set; } = new List<int>();

        public List<int> GetArmyByType(string armyType)
        {
            switch (armyType)
            {
                case "land": return LandArmy;
                case "sea": return SeaArmy;
                case "sky": return SkyArmy;
                default: return null;
            }
        }
    }
}
