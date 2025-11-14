using ArchsVsDinosServer.BusinessLogic.Game_Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.Game_Manager.Board
{
    public class CentralBoard
    {
        public List<string> LandArmy { get; set; } = new List<string>();
        public List<string> SeaArmy { get; set; } = new List<string>();
        public List<string> SkyArmy { get; set; } = new List<string>();

        public List<string> GetArmyByType(string armyType)
        {
            if (string.IsNullOrWhiteSpace(armyType))
            {
                return null;
            }

            switch (armyType.ToLower())
            {
                case "land":
                    return LandArmy;
                case "sea":
                    return SeaArmy;
                case "sky":
                    return SkyArmy;
                default:
                    return null;
            }
        }

        public int GetArmyPower(string armyType, CardHelper cardHelper)
        {
            var army = GetArmyByType(armyType);
            if (army == null || army.Count == 0)
            {
                return 0;
            }

            int totalPower = 0;
            foreach (var cardId in army)
            {
                var card = cardHelper.CreateCardInGame(cardId);
                if (card != null)
                {
                    totalPower += card.Power;
                }
            }

            return totalPower;
        }

        public void ClearArmy(string armyType)
        {
            var army = GetArmyByType(armyType);
            army?.Clear();
        }
    }
}
