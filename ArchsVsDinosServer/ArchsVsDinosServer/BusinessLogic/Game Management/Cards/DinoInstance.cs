using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.Game_Manager.Cards
{
    public class DinoInstance
    {
        private readonly List<CardInGame> bodyParts = new List<CardInGame>();

        public int DinoInstanceId { get; set; }
        public string ArmyType { get; set; }
        public CardInGame HeadCard { get; set; }
        public IReadOnlyList<CardInGame> BodyParts => bodyParts.AsReadOnly();

        public void AddBodyPart(CardInGame card)
        {
            bodyParts.Add(card);
        }

        public int GetTotalPower()
        {
            int total = HeadCard.Power;
            foreach (var part in bodyParts)
                total += part.Power;
            return total;
        }
    }
}
