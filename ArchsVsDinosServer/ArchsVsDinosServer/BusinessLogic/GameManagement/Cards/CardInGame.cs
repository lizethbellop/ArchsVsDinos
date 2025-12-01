using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement.Cards
{
    public class CardInGame
    {
        public int IdCard { get; set; }        
        public int Power { get; set; }
        public string Type { get; set; }     
        public string Element { get; set; }   
        public string BodyPart { get; set; }

        public static CardInGame FromDefinition(int idCard)
        {
            var cardDefinition = CardDefinitions.GetCard(idCard);
            if (cardDefinition == null) return null;

            return new CardInGame
            {
                IdCard = cardDefinition.IdCard,
                Power = cardDefinition.Power,
                Type = cardDefinition.Type,
                Element = cardDefinition.Element,
                BodyPart = cardDefinition.BodyPart
            };
        }

        public bool IsArch()
        {
            return Type == "arch";
        }

        public bool IsDinoHead()
        {
            return Type == "head";
        }

        public bool IsBodyPart()
        {
            return Type == "body";
        }
    }

}
