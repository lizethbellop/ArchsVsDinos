using ArchsVsDinosServer.BusinessLogic.Game_Manager.Board;
using ArchsVsDinosServer.BusinessLogic.Game_Manager.Cards;
using ArchsVsDinosServer.BusinessLogic.Game_Manager.Session;
using ArchsVsDinosServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.Game_Management
{
    public class GameActionHandler
    {
        private readonly CardHelper cardHelper;
        private readonly GameRulesValidator validator;
        private static int nextDinoId = 1;

        public GameActionHandler(ServiceDependencies dependencies)
        {
            cardHelper = new CardHelper(dependencies);
            validator = new GameRulesValidator();
        }

        public CardInGame DrawCard(GameSession session, PlayerSession player, int pileIndex)
        {
            if (session == null || player == null)
            {
                return null;
            }

            var drawnCards = session.DrawFromPile(pileIndex, 1);
            if (drawnCards == null || drawnCards.Count == 0)
            {
                return null;
            }

            var cardId = drawnCards[0];
            var card = cardHelper.CreateCardInGame(cardId);

            if (card == null)
            {
                return null;
            }

            // Si es un Arch (archLand, archSea, archSky), va directo al tablero
            if (ArmyTypeHelper.IsArch(card.ArmyType))
            {
                PlaceArchOnBoard(session.CentralBoard, cardId, card.ArmyType);
                session.MarkCardDrawn();
                return card; // Retornamos la carta para notificar
            }

            // Carta normal (Dino) va a la mano del jugador
            player.AddCard(card);
            session.MarkCardDrawn();

            return card;
        }

        public DinoInstance PlayDinoHead(GameSession session, PlayerSession player, string cardGlobalId)
        {
            if (session == null || player == null || string.IsNullOrWhiteSpace(cardGlobalId))
            {
                return null;
            }

            // Buscar la carta en la mano del jugador
            var card = player.Hand.FirstOrDefault(c => c.IdCardGlobal == cardGlobalId);
            if (card == null || !validator.IsValidDinoHead(card))
            {
                return null;
            }

            // Crear nuevo dino
            var dino = new DinoInstance
            {
                DinoInstanceId = nextDinoId++,
                HeadCard = card,
                ArmyType = card.ArmyType // dinoLand, dinoSea o dinoSky
            };

            // Remover carta de la mano y agregar dino al jugador
            player.RemoveCard(card);
            player.AddDino(dino);
            session.MarkCardPlayed();

            return dino;
        }

        public bool AttachBodyPart(GameSession session, PlayerSession player, string bodyCardId, string headCardId)
        {
            if (session == null || player == null ||
                string.IsNullOrWhiteSpace(bodyCardId) ||
                string.IsNullOrWhiteSpace(headCardId))
            {
                return false;
            }

            // Buscar la carta body en la mano
            var bodyCard = player.Hand.FirstOrDefault(c => c.IdCardGlobal == bodyCardId);
            if (bodyCard == null || !validator.IsValidBodyPart(bodyCard))
            {
                return false;
            }

            // Buscar el dino
            var dino = validator.FindDinoByHeadCardId(player, headCardId);
            if (dino == null || !validator.CanAttachBodyPart(bodyCard, dino))
            {
                return false;
            }

            // Adjuntar parte del cuerpo
            dino.AddBodyPart(bodyCard);
            player.RemoveCard(bodyCard);
            session.MarkCardPlayed();

            return true;
        }

        public PlayerSession GetNextPlayer(GameSession session)
        {
            if (session == null || !session.Players.Any())
            {
                return null;
            }

            var currentPlayer = session.Players.FirstOrDefault(p => p.UserId == session.CurrentTurn);
            if (currentPlayer == null)
            {
                return session.Players.First();
            }

            var currentIndex = session.Players.ToList().IndexOf(currentPlayer);
            var nextIndex = (currentIndex + 1) % session.Players.Count;

            return session.Players.ToList()[nextIndex];
        }

        private void PlaceArchOnBoard(CentralBoard board, string cardId, string armyType)
        {
            if (board == null || string.IsNullOrWhiteSpace(cardId) || string.IsNullOrWhiteSpace(armyType))
            {
                return;
            }

            string baseType = ArmyTypeHelper.GetBaseType(armyType);
            if (string.IsNullOrWhiteSpace(baseType))
            {
                return;
            }

            var army = board.GetArmyByType(baseType);
            if (army != null)
            {
                army.Add(cardId);
            }
        }
    }
}
