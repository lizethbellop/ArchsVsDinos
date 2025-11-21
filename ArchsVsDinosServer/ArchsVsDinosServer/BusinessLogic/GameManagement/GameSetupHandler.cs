using ArchsVsDinosServer.BusinessLogic.GameManagement.Board;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using ArchsVsDinosServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement
{
    public class GameSetupHandler
    {
        private readonly CardHelper cardHelper;
        private const int InitialHandSize = 5;

        public GameSetupHandler(ServiceDependencies dependencies)
        {
            cardHelper = new CardHelper(dependencies);
        }

        public bool InitializeGameSession(GameSession session, List<PlayerSession> players)
        {
            if (session == null || players == null || players.Count < 2 || players.Count > 4)
            {
                return false;
            }

            lock (session.SyncRoot)
            {
                int turnOrder = 1;
                foreach (var player in players)
                {
                    player.TurnOrder = turnOrder++;
                    session.AddPlayer(player);
                }

                var allCardIds = cardHelper.GetAllCardIds();
                var shuffledDeck = cardHelper.ShuffleCards(allCardIds);

                var remainingDeck = DealInitialHands(session, shuffledDeck);

                CreateSinglePile(session, remainingDeck);

                return true;
            }
        }

        private void CreateSinglePile(GameSession session, List<string> remainingCards)
        {
            var pile = new List<string>(remainingCards);
            session.SetDrawPiles(new List<List<string>> { pile });
        }

        private List<string> DealInitialHands(GameSession session, List<string> deck)
        {
            var deckCopy = new List<string>(deck);
            var currentIndex = 0;

            foreach (var player in session.Players)
            {
                var playerHand = new List<CardInGame>();

                // Repartir cartas iniciales
                while (playerHand.Count < InitialHandSize && currentIndex < deckCopy.Count)
                {
                    var cardId = deckCopy[currentIndex];
                    currentIndex++;

                    var card = cardHelper.CreateCardInGame(cardId);
                    if (card == null)
                    {
                        continue;
                    }

                    // Si es un Arch (archLand, archSea, archSky), ponerlo en el tablero central
                    if (ArmyTypeHelper.IsArch(card.ArmyType))
                    {
                        PlaceArchOnBoard(session.CentralBoard, cardId, card.ArmyType);
                        // No cuenta como carta en mano, seguir repartiendo
                        continue;
                    }

                    // Agregar carta normal (Dino) a la mano
                    player.AddCard(card);
                    playerHand.Add(card);
                }
            }

            return deckCopy.Skip(currentIndex).ToList();
        }

        private void PlaceArchOnBoard(CentralBoard board, string cardId, string armyType)
        {
            if (board == null || string.IsNullOrWhiteSpace(cardId) || string.IsNullOrWhiteSpace(armyType))
            {
                return;
            }

            var baseType = ArmyTypeHelper.GetBaseType(armyType);
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

        public PlayerSession SelectFirstPlayer(GameSession session)
        {
            if (session == null || !session.Players.Any())
            {
                return null;
            }

            var players = session.Players.ToList();
            var randomIndex = GetSecureRandomNumber(players.Count);

            return players[randomIndex];
        }

        private int GetSecureRandomNumber(int maxValue)
        {
            if (maxValue <= 0)
            {
                return 0;
            }

            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                var value = BitConverter.ToUInt32(bytes, 0);
                return (int)(value % (uint)maxValue);
            }
        }
    }
}
