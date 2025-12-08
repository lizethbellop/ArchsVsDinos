using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using System.Linq;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement
{
    public class GameRulesValidator
    {
        private const int MaxCardsPerTurn = 2;
        private const int MaxActionsPerTurn = 3;

        public bool CanDrawCard(GameSession session, int userId)
        {
            if (session == null || session.CurrentTurn != userId)
            {
                return false;
            }

            var totalActions = session.CardsDrawnThisTurn + session.CardsPlayedThisTurn;
            return totalActions < MaxActionsPerTurn;
        }

        public bool CanPlayCard(GameSession session, int userId)
        {
            if (session == null || session.CurrentTurn != userId)
            {
                return false;
            }

            if (session.CardsPlayedThisTurn >= MaxCardsPerTurn)
            {
                return false;
            }

            var totalActions = session.CardsDrawnThisTurn + session.CardsPlayedThisTurn;
            return totalActions < MaxActionsPerTurn;
        }

        public bool CanProvoke(GameSession session, int userId)
        {
            if (session == null || session.CurrentTurn != userId)
            {
                return false;
            }

            return session.CardsDrawnThisTurn == 0 &&
                   session.CardsPlayedThisTurn == 0 &&
                   !session.HasTakenMainAction;
        }

        public bool CanEndTurn(GameSession session, int userId)
        {
            return session != null && session.CurrentTurn == userId;
        }

        public bool IsValidDinoHead(CardInGame card)
        {
            return card != null && card.Type == "head" && card.IsDinoHead();
        }

        public bool IsValidBodyPart(CardInGame card)
        {
            return card != null && card.Type == "body" && card.IsBodyPart();
        }

        public bool CanAttachBodyPart(CardInGame bodyCard, DinoInstance dino)
        {
            if (bodyCard == null || dino == null || dino.HeadCard == null)
            {
                return false;
            }

            var normalizedBodyElement = ArmyTypeHelper.NormalizeElement(bodyCard.Element);
            var normalizedDinoElement = ArmyTypeHelper.NormalizeElement(dino.Element);

            return normalizedBodyElement == normalizedDinoElement;
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
            return ArmyTypeHelper.IsValidBaseType(armyType);
        }
    }
}