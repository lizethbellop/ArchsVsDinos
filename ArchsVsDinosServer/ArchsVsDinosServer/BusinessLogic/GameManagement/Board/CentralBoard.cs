using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement.Board
{
    public class CentralBoard
    {
        public List<int> SandArmy { get; set; } = new List<int>();
        public List<int> WaterArmy { get; set; } = new List<int>();
        public List<int> WindArmy { get; set; } = new List<int>();

        public List<int> GetArmyByType(string element)
        {
            if (string.IsNullOrWhiteSpace(element))
            {
                return null;
            }

            switch (element.ToLower())
            {
                case "sand":
                    return SandArmy;
                case "water":
                    return WaterArmy;
                case "wind":
                    return WindArmy;
                default:
                    return null;
            }
        }

        public int GetArmyPower(string element)
        {
            var army = GetArmyByType(element);
            if (army == null || army.Count == 0)
            {
                return 0;
            }

            int totalPower = 0;
            foreach (var cardId in army)
            {
                var card = CardInGame.FromDefinition(cardId);
                if (card != null)
                {
                    totalPower += card.Power;
                }
            }

            return totalPower;
        }

        public void ClearArmy(string element)
        {
            var army = GetArmyByType(element);
            army?.Clear();
        }
    }
}