using Contracts.DTO.Game_DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement.Cards
{
    public static class CardConverter
    {

        public static CardDTO ToDTO(CardInGame card)
        {
            if (card == null)
            {
                return null;
            }

            return new CardDTO
            {
                IdCard = card.IdCard,
                Power = card.Power,
                Type = card.Type,
                Element = card.Element,
                BodyPart = card.BodyPart
            };
        }

        public static List<CardDTO> ToDTOList(List<CardInGame> cards)
        {
            if (cards == null)
            {
                return new List<CardDTO>();
            }

            var cardDTOList = cards
                .Select(ToDTO)
                .Where(cardDTO => cardDTO != null)
                .ToList();

            return cardDTOList;
        }

    }
}
