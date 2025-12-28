using ArchsVsDinosServer.BusinessLogic.GameManagement.Board;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Session;
using Contracts.DTO.Game_DTO.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
                Console.WriteLine($"[SETUP] Total cards from definitions: {allCardIds.Count}");
                var shuffledDeck = CardShuffler.ShuffleCards(allCardIds);
                Console.WriteLine($"[SETUP] Shuffled deck size: {shuffledDeck.Count}");

                var duplicates = shuffledDeck.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                if (duplicates.Any())
                {
                    Console.WriteLine($"[ERROR] Duplicate card IDs found: {string.Join(", ", duplicates)}");
                }

                // Verificar IDs inválidos
                var invalidIds = shuffledDeck.Where(id => id < 1 || id > 96).ToList();
                if (invalidIds.Any())
                {
                    Console.WriteLine($"[ERROR] Invalid card IDs found: {string.Join(", ", invalidIds)}");
                }

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
            session.SetDrawDeck(remainingCards);
        }

        private List<int> DealInitialHands(GameSession session, List<int> deck)
        {
            var deckQueue = new Queue<int>(deck);

            foreach (var player in session.Players)
            {
                int archCount = 0;
                int cardCount = 0;

                while (player.Hand.Count < InitialHandSize && deckQueue.Count > 0)
                {
                    int cardId = deckQueue.Dequeue();
                    var card = CardInGame.FromDefinition(cardId);

                    if (card == null) continue;

                    if (card.IsArch())
                    {
                        PlaceArchOnBoard(session.CentralBoard, card);
                        archCount++;
                    }
                    else
                    {
                        player.AddCard(card);
                        cardCount++;
                    }
                }
                Console.WriteLine($"[SETUP] Player {player.UserId} - Hand: {player.Hand.Count}, Archs: {archCount}, Total: {cardCount + archCount}");
            }

            return deckQueue.ToList();
        }

        private void PlaceArchOnBoard(CentralBoard board, CardInGame archCard)
        {
            if (board == null || archCard == null || !archCard.IsArch() || archCard.Element == ArmyType.None)
            {
                return;
            }

            board.AddArchCardToArmy(archCard);
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