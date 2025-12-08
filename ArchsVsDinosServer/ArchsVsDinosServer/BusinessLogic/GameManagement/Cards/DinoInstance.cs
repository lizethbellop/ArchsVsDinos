using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement.Cards
{
    public class DinoInstance
    {
        private readonly List<CardInGame> bodyParts = new List<CardInGame>();

        public int DinoInstanceId { get; set; }
        public int IdDino { get; set; }
        public CardInGame HeadCard { get; set; }
        public string Element { get; set; }
        public CardInGame ChestCard { get; set; }
        public CardInGame LeftArmCard { get; set; }
        public CardInGame RightArmCard { get; set; }
        public CardInGame LegsCard { get; set; }

        public IReadOnlyList<CardInGame> BodyParts => bodyParts.AsReadOnly();

        public void AddBodyPart(CardInGame card)
        {
            if (card != null && card.IsBodyPart())
            {
                bodyParts.Add(card);
            }
        }

        public int TotalPower
        {
            get
            {
                int power = HeadCard?.Power ?? 0;
                power += ChestCard?.Power ?? 0;
                power += LeftArmCard?.Power ?? 0;
                power += RightArmCard?.Power ?? 0;
                power += LegsCard?.Power ?? 0;
                return power;
            }
        }
    }
}
