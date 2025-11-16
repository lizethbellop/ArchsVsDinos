using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Models
{
    public class Card
    {
        public int IdCard { get; set; }
        public string CardRoute { get; set; }
        public int Power { get; set; }
        public CardCategory Category { get; set; }
        public ElementType Element { get; set; } = ElementType.None;
        public BodyPartType BodyPartType { get; set; } = BodyPartType.None;
        public ChestSubtype ChestSubtype { get; set; } = ChestSubtype.None;


        public Card(int id, string route, int power)
        {
            IdCard = id;
            CardRoute = route;
            Power = power;
            DeduceElementFromRoute(route);
            DeduceCategoryFromRoute(route);
            DeduceBodyPartFromRoute(route);
        }

        public bool CanAttach(Card other)
        {
            if (other == null) 
                return false;

            if (Category == CardCategory.Arch)
                return false;

            switch (Category)
            {
                case CardCategory.DinoHead:
                    return other.BodyPartType == BodyPartType.Chest;

                case CardCategory.BodyPart:
                    return CanBodyPartAttach(other);

                default:
                    return false;
            }
        }

        private bool CanBodyPartAttach(Card other)
        {
            if (BodyPartType != BodyPartType.Chest)
                return false;

            switch (ChestSubtype)
            {
                case ChestSubtype.Complete:
                    return false;
                case ChestSubtype.Arms:
                    return other.BodyPartType == BodyPartType.LeftArm ||
                           other.BodyPartType == BodyPartType.RightArm;
                case ChestSubtype.ArmsLegs:
                    return other.BodyPartType == BodyPartType.LeftArm ||
                           other.BodyPartType == BodyPartType.RightArm ||
                           other.BodyPartType == BodyPartType.Legs;
                default:
                    return false;
            }
        }

        private void DeduceElementFromRoute(string route)
        {
            var elementRules = new Dictionary<string, ElementType>
            {
                { "Sea", ElementType.Sea },
                { "Sand", ElementType.Sand },
                { "Wind", ElementType.Wind },
                { "None", ElementType.None },
            };

            foreach (var rule in elementRules)
            {
                if (route.Contains(rule.Key))
                {
                    Element = rule.Value;
                    return;
                }
            }

            Element = ElementType.None;
        }

        private void DeduceCategoryFromRoute(string route)
        {
            if (route.Contains("Archs"))
            {
                Category = CardCategory.Arch;
                return;
            }

            if (route.Contains("Dinos"))
            {
                Category = CardCategory.DinoHead;
                return;
            }

            Category = CardCategory.BodyPart;
        }

        private void DeduceBodyPartFromRoute(string route)
        {
            var bodyPartRules = new List<(string Key, BodyPartType Part, ChestSubtype Chest)>
            {
                ("ChestComplete", BodyPartType.Chest, ChestSubtype.Complete),
                ("ChestArmsLegs", BodyPartType.Chest, ChestSubtype.ArmsLegs),
                ("ChestArms", BodyPartType.Chest, ChestSubtype.Arms),

                ("LeftArm", BodyPartType.LeftArm, ChestSubtype.None),
                ("RightArm", BodyPartType.RightArm, ChestSubtype.None),
                ("Legs", BodyPartType.Legs, ChestSubtype.None),
            };

            foreach (var rule in bodyPartRules)
            {
                if (route.Contains(rule.Key))
                {
                    BodyPartType = rule.Part;
                    ChestSubtype = rule.Chest;
                    return;
                }
            }
        }
    }
}
