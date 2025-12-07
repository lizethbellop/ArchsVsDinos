using ArchsVsDinosServer.BusinessLogic.GameManagement.Board;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement
{
    public class GameSetupHandler
    {
        private const int InitialHandSize = 5;

        public bool InitializeGameSession(GameSession session, List<PlayerSession> players)
        {
            if (session == null || players == null || players.Count < 2 || players.Count > 4)
            {
                return false;
            }

            lock (session.SyncRoot)
            {
                foreach (var player in players)
                {
                    player.ClearHand();
                    player.ClearDinos();
                }

                if (session.Players.Count == 0)
                {
                    AssignTurnOrderToPlayers(session, players);
                }

                var allCardIds = CardDefinitions.GetAllCardIds();
                var shuffledDeck = CardShuffler.ShuffleCards(allCardIds);
                var remainingDeck = DealInitialHands(session, shuffledDeck);

                CreateSingleDrawPile(session, remainingDeck);

                return true;
            }
        }

        private void AssignTurnOrderToPlayers(GameSession session, List<PlayerSession> players)
        {
            int turnOrder = 1;
            foreach (var player in players)
            {
                player.TurnOrder = turnOrder++;
                session.AddPlayer(player);
            }
        }

        private void CreateSingleDrawPile(GameSession session, List<int> remainingCards)
        {
            var drawPile = new List<int>(remainingCards);
            var drawPiles = new List<List<int>> { drawPile };
            session.SetDrawPiles(drawPiles);
        }

        private List<int> DealInitialHands(GameSession session, List<int> deck)
        {
            var deckQueue = new Queue<int>(deck);

            foreach (var player in session.Players)
            {
                while (player.Hand.Count < InitialHandSize && deckQueue.Count > 0)
                {
                    int cardId = deckQueue.Dequeue();
                    var card = CardInGame.FromDefinition(cardId);

                    if (card == null) continue;

                    if (card.IsArch())
                    {
                        PlaceArchOnBoard(session.CentralBoard, cardId, card.Element);
                    }
                    else
                    {
                        player.AddCard(card);
                    }
                }
            }

            return deckQueue.ToList();
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

        public PlayerSession SelectFirstPlayer(GameSession session)
        {
            if (session == null || !session.Players.Any())
            {
                return null;
            }

            var playersList = session.Players.ToList();

            using (var randomGenerator = RandomNumberGenerator.Create())
            {
                var randomPlayerIndex = GetSecureRandomNumber(randomGenerator, playersList.Count);
                return playersList[randomPlayerIndex];
            }
        }

        private int GetSecureRandomNumber(RandomNumberGenerator randomGenerator, int maxValue)
        {
            if (maxValue <= 0)
            {
                return 0;
            }

            var randomBytes = new byte[4];
            randomGenerator.GetBytes(randomBytes);
            var randomValue = BitConverter.ToUInt32(randomBytes, 0);
            return (int)(randomValue % (uint)maxValue);
        }
    }
}