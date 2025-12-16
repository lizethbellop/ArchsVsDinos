using System;
using System.Collections.Generic;
using Contracts.DTO.Game_DTO.Enums;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement.Cards
{
    public class DinoInstance
    {
        private readonly object syncRoot = new object();

        public int DinoInstanceId { get; }
        public ArmyType Element { get; }

        public CardInGame HeadCard { get; }

        private CardInGame torsoCard;
        private CardInGame leftArmCard;
        private CardInGame rightArmCard;
        private CardInGame legsCard;

        public CardInGame TorsoCard => torsoCard;
        public CardInGame LeftArmCard => leftArmCard;
        public CardInGame RightArmCard => rightArmCard;
        public CardInGame LegsCard => legsCard;

        public DinoInstance(int dinoInstanceId, CardInGame headCard)
        {
            if (headCard == null)
                throw new ArgumentNullException(nameof(headCard));

            if (headCard.PartType != DinoPartType.Head)
                throw new ArgumentException("HeadCard must be of type Head.");

            if (headCard.Element == ArmyType.None)
                throw new ArgumentException("HeadCard must have a defined ArmyType.");

            DinoInstanceId = dinoInstanceId;
            HeadCard = headCard;
            Element = headCard.Element;
        }

        public bool TryAddBodyPart(CardInGame card)
        {
            lock (syncRoot)
            {
                if (card == null || !card.IsBodyPart()) return false;

                switch (card.PartType)
                {
                    case DinoPartType.Torso:
                        if (torsoCard == null) { torsoCard = card; return true; }
                        break;
                    case DinoPartType.Legs:
                        if (legsCard == null) { legsCard = card; return true; }
                        break;
                    case DinoPartType.Arms:
                        if (leftArmCard == null) { leftArmCard = card; return true; }
                        if (rightArmCard == null) { rightArmCard = card; return true; }
                        break;
                }
                return false;
            }
        }

        public int TotalPower => HeadCard?.Power ?? 0
            + (torsoCard?.Power ?? 0)
            + (leftArmCard?.Power ?? 0)
            + (rightArmCard?.Power ?? 0)
            + (legsCard?.Power ?? 0);

        public IReadOnlyList<CardInGame> GetAllCards()
        {
            var list = new List<CardInGame> { HeadCard };
            if (torsoCard != null) list.Add(torsoCard);
            if (leftArmCard != null) list.Add(leftArmCard);
            if (rightArmCard != null) list.Add(rightArmCard);
            if (legsCard != null) list.Add(legsCard);
            return list.AsReadOnly();
        }

        public int ArchaeologistsEliminated
        {
            get
            {
                return GetAllCards()
                    .Where(c => c.PartType == DinoPartType.None && c.Element != ArmyType.None && c.Power < 10)
                    .Sum(c => c.Power);
            }
        }

        public int SupremeBossesEliminated
        {
            get
            {
                return GetAllCards()
                    .Where(c => c.PartType == DinoPartType.None && c.Element != ArmyType.None && c.Power >= 10)
                    .Sum(c => c.Power);
            }
        }
    }

}
