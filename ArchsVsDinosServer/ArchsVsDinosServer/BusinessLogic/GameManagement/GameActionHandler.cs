using ArchsVsDinosServer.BusinessLogic.GameManagement.Board;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using ArchsVsDinosServer.Utils;
using Contracts.DTO.Game_DTO.Enums;
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
            if (session == null || player == null) return null;

            if (!session.ConsumeMoves(1)) return null;

            var card = ProcessDrawnCard(session, player, pileIndex);

            if (card == null)
            {
                session.RestoreMoves(1);
            }

            return card;
        }

        public DinoInstance PlayDinoHead(GameSession session, PlayerSession player, int cardId)
        {
            if (!session.ConsumeMoves(1)) return null;

            var cardInHand = player.RemoveCardById(cardId);

            if (cardInHand == null || !cardInHand.IsDinoHead())
            {
                session.RestoreMoves(1);
                return null;
            }

            var newDino = new DinoInstance(
                dinoInstanceId: nextDinoId++,
                headCard: cardInHand
            );

            player.AddDino(newDino);

            return newDino;
        }

        public bool AttachBodyPart(GameSession session, PlayerSession player, int bodyCardId, int dinoHeadCardId)
        {
            if (!session.ConsumeMoves(1)) return false;

            var targetDino = player.GetDinoByHeadCardId(dinoHeadCardId);
            var bodyCard = player.GetCardById(bodyCardId);

            if (bodyCard == null || targetDino == null || !bodyCard.IsBodyPart())
            {
                session.RestoreMoves(1);
                return false;
            }

            var successfullyAttached = targetDino.TryAddBodyPart(bodyCard);

            if (successfullyAttached)
            {
                player.RemoveCard(bodyCard);
                return true;
            }

            session.RestoreMoves(1);
            return false;
        }

        public CardInGame ExchangeCard(GameSession session, PlayerSession player, int cardIdToDiscard, int pileIndex)
        {
            if (session == null || player == null) return null;

            if (!session.ConsumeMoves(1)) return null;

            var cardToDiscard = player.RemoveCardById(cardIdToDiscard);

            if (cardToDiscard == null)
            {
                session.RestoreMoves(1);
                return null;
            }

            session.AddToDiscard(cardToDiscard.IdCard);

            var newCard = ProcessDrawnCard(session, player, pileIndex);

            return newCard;
        }

        private CardInGame ProcessDrawnCard(GameSession session, PlayerSession player, int pileIndex)
        {
            var drawnCardIds = session.DrawFromPile(pileIndex, 1);
            if (drawnCardIds == null || drawnCardIds.Count == 0)
            {
                return null;
            }

            var cardId = drawnCardIds[0];
            var card = CardInGame.FromDefinition(cardId);

            if (card == null) return null;

            if (card.IsArch())
            {
                PlaceArchOnBoard(session.CentralBoard, card);
            }
            else
            {
                player.AddCard(card);
            }
            return card;
        }

        private void PlaceArchOnBoard(CentralBoard board, CardInGame archCard)
        {
            if (board == null || archCard == null || !archCard.IsArch() || archCard.Element == ArmyType.None) return;
            board.AddArchCardToArmy(archCard);
        }

        public PlayerSession GetNextPlayer(GameSession session)
        {
            if (session == null || !session.Players.Any()) return null;

            var currentPlayer = session.Players.FirstOrDefault(player => player.UserId == session.CurrentTurn);
            if (currentPlayer == null) return session.Players.OrderBy(p => p.TurnOrder).First();

            var playersList = session.Players.OrderBy(p => p.TurnOrder).ToList();
            var currentPlayerIndex = playersList.IndexOf(currentPlayer);
            var nextPlayerIndex = (currentPlayerIndex + 1) % playersList.Count;

            return playersList[nextPlayerIndex];
        }
    }
}