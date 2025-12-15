using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using Contracts.DTO.Game_DTO.Enums;
using System;
using System.Linq;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement
{
    public class GameRulesValidator
    {
        private const int CostPerAction = 1;
        private const int CostProvoke = 3;

        public bool CanTakeOneCostAction(GameSession session, int userId)
        {
            if (session == null || session.CurrentTurn != userId)
            {
                return false;
            }

            return session.RemainingMoves >= CostPerAction;
        }

        public bool CanProvoke(GameSession session, int userId)
        {
            if (session == null || session.CurrentTurn != userId)
            {
                return false;
            }

            return session.RemainingMoves >= CostProvoke;
        }

        public bool CanEndTurn(GameSession session, int userId)
        {
            return session != null && session.CurrentTurn == userId;
        }

        public bool IsValidDinoHead(CardInGame card)
        {
            return card != null && card.PartType == DinoPartType.Head;
        }

        public bool IsValidBodyPart(CardInGame card)
        {
            return card != null && card.IsBodyPart();
        }

        public bool CanAttachBodyPart(CardInGame bodyCard, DinoInstance dino)
        {
            if (bodyCard == null || dino == null || dino.HeadCard == null)
            {
                return false;
            }

            var bodyElement = bodyCard.Element;
            var dinoElement = dino.Element;

            return bodyElement == ArmyType.None || bodyElement == dinoElement;
        }

        public bool PlayerHasCard(PlayerSession player, int cardId)
        {
            if (player == null || cardId <= 0)
            {
                return false;
            }

            return player.Hand.Any(card => card.IdCard == cardId);
        }

        public DinoInstance FindDinoByHeadCardId(PlayerSession player, int headCardId)
        {
            if (player == null || headCardId <= 0)
            {
                return null;
            }

            return player.Dinos.FirstOrDefault(dino => dino.HeadCard?.IdCard == headCardId);
        }

        public bool IsValidArmyType(string armyType)
        {
            ArmyType result;
            return Enum.TryParse(armyType, true, out result) && result != ArmyType.None;
        }
    }
}