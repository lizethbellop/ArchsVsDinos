using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using System;
using System.Collections.Generic;
using Contracts.DTO.Game_DTO.Enums;
using System.Linq;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement.Board
{
    public class CentralBoard
    {
        private readonly object syncRoot = new object();

        private readonly List<int> sandArmy = new List<int>();
        private readonly List<int> waterArmy = new List<int>();
        private readonly List<int> windArmy = new List<int>();

        private CardInGame supremeBossCard;

        public IReadOnlyList<int> SandArmy => sandArmy.AsReadOnly();
        public IReadOnlyList<int> WaterArmy => waterArmy.AsReadOnly();
        public IReadOnlyList<int> WindArmy => windArmy.AsReadOnly();
        public CardInGame SupremeBossCard => supremeBossCard;

        public List<int> GetArmyByType(ArmyType type)
        {
            lock (syncRoot)
            {
                switch (type)
                {
                    case ArmyType.Sand:
                        return sandArmy;
                    case ArmyType.Water:
                        return waterArmy;
                    case ArmyType.Wind:
                        return windArmy;
                    default:
                        return null;
                }
            }
        }

        public void AddArchCardToArmy(CardInGame archCard)
        {
            if (archCard == null || archCard.Element == ArmyType.None) return;

            lock (syncRoot)
            {
                var army = GetArmyByType(archCard.Element);
                if (army != null)
                {
                    army.Add(archCard.IdCard);
                }
            }
        }

        public void SetSupremeBoss(CardInGame bossCard)
        {
            lock (syncRoot)
            {
                supremeBossCard = bossCard;
            }
        }

        public CardInGame RemoveSupremeBoss()
        {
            lock (syncRoot)
            {
                var boss = supremeBossCard;
                supremeBossCard = null;
                return boss;
            }
        }

        public int GetArmyPower(ArmyType type)
        {
            var army = GetArmyByType(type);
            if (army == null || army.Count == 0) return 0;

            int totalPower = 0;

            lock (syncRoot)
            {
                foreach (var cardId in army)
                {
                    var card = CardInGame.FromDefinition(cardId);
                    
                    if (card != null)
                    {
                        totalPower += card.Power;
                    }
                }
                
                if (SupremeBossCard != null && SupremeBossCard.Element == type)
                {
                    totalPower += SupremeBossCard.Power;
                }
            }

            return totalPower;
        }

        public List<int> ClearArmy(ArmyType type)
        {
            lock (syncRoot)
            {
                var army = GetArmyByType(type);
                if (army != null)
                {
                    var discardedCards = new List<int>(army);
                    army.Clear();
                    return discardedCards;
                }
                return new List<int>();
            }
        }
    }
}