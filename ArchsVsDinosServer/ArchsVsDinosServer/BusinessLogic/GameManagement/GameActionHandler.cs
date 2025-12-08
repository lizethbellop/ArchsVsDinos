using ArchsVsDinosServer.BusinessLogic.GameManagement.Board;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using ArchsVsDinosServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement
{
    public class GameActionHandler
    {
        private readonly GameRulesValidator validator;
        private static int nextDinoId = 1;

        public GameActionHandler(ServiceDependencies dependencies)
        {
            validator = new GameRulesValidator();
        }

        public CardInGame DrawCard(GameSession session, PlayerSession player, int pileIndex)
        {
            if (session == null || player == null)
            {
                return null;
            }

            var drawnCardIds = session.DrawFromPile(pileIndex, 1);
            if (drawnCardIds == null || drawnCardIds.Count == 0)
            {
                return null;
            }

            var cardId = drawnCardIds[0];
            var card = CardInGame.FromDefinition(cardId);

            if (card == null)
            {
                return null;
            }

            if (card.IsArch())
            {
                PlaceArchOnBoard(session.CentralBoard, cardId, card.Element);
                session.MarkCardDrawn();
                return card;
            }

            player.AddCard(card);
            session.MarkCardDrawn();

            return card;
        }

        public DinoInstance PlayDinoHead(GameSession session, PlayerSession player, int cardId)
        {
            if (!session.ConsumeMove()) return null;

            var cardInHand = player.Hand.FirstOrDefault(c => c.IdCard == cardId);
            if (cardInHand == null)
            {
                session.RestoreMove();
                return null;
            }

            var newDino = new DinoInstance
            {
                IdDino = nextDinoId++,
                HeadCard = cardInHand,
                Element = cardInHand.Element
            };

            player.Hand.Remove(cardInHand);
            player.Dinos.Add(newDino);

            session.MarkCardPlayed();
            return newDino;
        }

        public bool AttachBodyPart(GameSession session, PlayerSession player, int bodyCardId, int dinoHeadCardId)
        {
            if (!session.ConsumeMove()) return false;

            var bodyCard = player.Hand.FirstOrDefault(card => card.IdCard == bodyCardId);
            var targetDino = player.Dinos.FirstOrDefault(card => card.HeadCard.IdCard == dinoHeadCardId);

            if (bodyCard == null || targetDino == null)
            {
                session.RestoreMove();
                return false;
            }

            switch (bodyCard.Type) 
            {
                case "chest":
                    if (targetDino.ChestCard != null) { session.RestoreMove(); return false; }
                    targetDino.ChestCard = bodyCard;
                    break;
                case "left_arm":
                    if (targetDino.LeftArmCard != null) { session.RestoreMove(); return false; }
                    targetDino.LeftArmCard = bodyCard;
                    break;
                // ... (otros casos: right_arm, legs)
                default:
                    session.RestoreMove();
                    return false;
            }

            player.Hand.Remove(bodyCard);
            session.MarkCardPlayed();

            return true;
        }

        public PlayerSession GetNextPlayer(GameSession session)
        {
            if (session == null || !session.Players.Any())
            {
                return null;
            }

            var currentPlayer = session.Players.FirstOrDefault(player => player.UserId == session.CurrentTurn);
            if (currentPlayer == null)
            {
                return session.Players.First();
            }

            var playersList = session.Players.ToList();
            var currentPlayerIndex = playersList.IndexOf(currentPlayer);
            var nextPlayerIndex = (currentPlayerIndex + 1) % session.Players.Count;

            return playersList[nextPlayerIndex];
        }

        private void PlaceArchOnBoard(CentralBoard board, int cardId, string element)
        {
            if (board == null || cardId <= 0 || string.IsNullOrWhiteSpace(element))
            {
                return;
            }

            var normalizedElement = ArmyTypeHelper.NormalizeElement(element);
            var armyList = board.GetArmyByType(normalizedElement);

            if (armyList != null)
            {
                armyList.Add(cardId);
            }
        }
    }
}