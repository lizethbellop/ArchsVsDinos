using System;
using Contracts.DTO.Game_DTO.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement.Cards
{
    public class CardInGame
    {
        public int IdCard { get; private set; }
        public int Power { get; private set; }

        public ArmyType Element { get; private set; }
        public DinoPartType PartType { get; private set; }

        public bool HasTopJoint { get; private set; }
        public bool HasBottomJoint { get; private set; }
        public bool HasLeftJoint { get; private set; }
        public bool HasRightJoint { get; private set; }

        public CardInGame() { }

        public static CardInGame FromDefinition(int idCard)
        {
            var def = CardDefinitions.GetCard(idCard);
            if (def == null) return null;

            var newCard = new CardInGame
            {
                IdCard = def.IdCard,
                Power = def.Power,
                Element = ParseElement(def.Element),
                PartType = ParsePartType(def.BodyPart, def.Type)
            };

            AssignJoints(newCard);

            return newCard;
        }

        private static ArmyType ParseElement(string element)
        {

            ArmyType result;
            if (Enum.TryParse(element, true, out result))
            {
                return result;
            }
            return ArmyType.None;
        }

        private static DinoPartType ParsePartType(string bodyPart, string type)
        {
            if (type == "head") return DinoPartType.Head;

            DinoPartType result;

            switch (bodyPart)
            {
                case "Chest": result = DinoPartType.Torso; break;
                case "Legs": result = DinoPartType.Legs; break;
                case "LeftArm":
                case "RightArm": result = DinoPartType.Arms; break;
                default: result = DinoPartType.None; break;
            }
            return result;
        }

        private static void AssignJoints(CardInGame card)
        {
            switch (card.PartType)
            {
                case DinoPartType.Head:
                    card.HasBottomJoint = true;
                    break;
                case DinoPartType.Torso:
                    card.HasTopJoint = true;
                    card.HasBottomJoint = true;
                    card.HasLeftJoint = true;
                    card.HasRightJoint = true;
                    break;
                case DinoPartType.Legs:
                    card.HasTopJoint = true;
                    break;
                case DinoPartType.Arms:
                    card.HasLeftJoint = true;
                    card.HasRightJoint = true;
                    break;
                default:
                    break;
            }
        }

        public bool IsArch()
        {
            return PartType == DinoPartType.None && Element != ArmyType.None;
        }

        public bool IsDinoHead()
        {
            return PartType == DinoPartType.Head;
        }

        public bool IsBodyPart()
        {
            return PartType == DinoPartType.Torso ||
                   PartType == DinoPartType.Legs ||
                   PartType == DinoPartType.Arms;
        }
    }

}
