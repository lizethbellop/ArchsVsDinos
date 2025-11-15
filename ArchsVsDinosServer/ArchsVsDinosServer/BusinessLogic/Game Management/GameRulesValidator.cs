using ArchsVsDinosServer.BusinessLogic.Game_Manager.Cards;
using ArchsVsDinosServer.BusinessLogic.Game_Manager.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.Game_Management
{
    public class GameRulesValidator
    {
        private const int MaxCardsPerTurn = 2;
        private const int MaxActionsPerTurn = 2;

        public bool CanDrawCard(GameSession session, int userId)
        {
            if (session == null || session.CurrentTurn != userId)
            {
                return false;
            }

            if (session.HasDrawnThisTurn && session.CardsPlayedThisTurn >= MaxCardsPerTurn)
            {
                return false;
            }

            var totalActions = (session.HasDrawnThisTurn ? 1 : 0) + session.CardsPlayedThisTurn;
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

            var totalActions = (session.HasDrawnThisTurn ? 1 : 0) + session.CardsPlayedThisTurn;
            return totalActions < MaxActionsPerTurn;
        }

        public bool CanProvoke(GameSession session, int userId)
        {
            if (session == null || session.CurrentTurn != userId)
            {
                return false;
            }

            // Para provocar, no debes haber tomado ninguna acción
            return !session.HasDrawnThisTurn &&
                   session.CardsPlayedThisTurn == 0 &&
                   !session.HasTakenMainAction;
        }

        public bool CanEndTurn(GameSession session, int userId)
        {
            return session != null && session.CurrentTurn == userId;
        }

        public bool IsValidDinoHead(CardInGame card)
        {
            return card != null &&
                   card.Type == "head" &&
                   !string.IsNullOrWhiteSpace(card.ArmyType);
        }

        public bool IsValidBodyPart(CardInGame card)
        {
            return card != null &&
                   card.Type == "body" &&
                   !string.IsNullOrWhiteSpace(card.ArmyType);
        }

        public bool CanAttachBodyPart(CardInGame bodyCard, DinoInstance dino)
        {
            if (bodyCard == null || dino == null || dino.HeadCard == null)
            {
                return false;
            }

            // El body debe ser del mismo tipo de ejército que el dino
            return bodyCard.ArmyType == dino.ArmyType;
        }

        public bool PlayerHasCard(PlayerSession player, string cardGlobalId)
        {
            if (player == null || string.IsNullOrWhiteSpace(cardGlobalId))
            {
                return false;
            }

            return player.Hand.Any(c => c.IdCardGlobal == cardGlobalId);
        }

        public DinoInstance FindDinoByHeadCardId(PlayerSession player, string headCardId)
        {
            if (player == null || string.IsNullOrWhiteSpace(headCardId))
            {
                return null;
            }

            return player.Dinos.FirstOrDefault(d => d.HeadCard?.IdCardGlobal == headCardId);
        }

        public bool IsValidArmyType(string armyType)
        {
            return armyType == "land" || armyType == "sea" || armyType == "sky";
        }
    }
}
