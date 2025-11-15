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
    public class GameSetupHandler
    {
        private readonly CardHelper cardHelper;
        private const int InitialHandSize = 5;
        private const int NumberOfDrawPiles = 3;

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
                // Agregar jugadores a la sesión
                int turnOrder = 1;
                foreach (var player in players)
                {
                    player.TurnOrder = turnOrder++;
                    session.AddPlayer(player);
                }

                // Obtener todas las cartas y barajarlas
                var allCardIds = cardHelper.GetAllCardIds();
                var shuffledDeck = cardHelper.ShuffleCards(allCardIds);

                // Repartir manos iniciales y procesar Archs (bebés)
                var remainingDeck = DealInitialHands(session, shuffledDeck);

                // Dividir el mazo restante en 3 pilas
                CreateDrawPiles(session, remainingDeck);

                return true;
            }
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

                    // Si es un Arch (bebé), ponerlo en el tablero central
                    if (IsArchBaby(card))
                    {
                        PlaceArchOnBoard(session.CentralBoard, card);
                        // No cuenta como carta en mano, seguir repartiendo
                        continue;
                    }

                    // Agregar carta normal a la mano
                    player.AddCard(card);
                    playerHand.Add(card);
                }
            }

            // Retornar las cartas restantes
            return deckCopy.Skip(currentIndex).ToList();
        }

        private void CreateDrawPiles(GameSession session, List<string> remainingCards)
        {
            var piles = new List<List<string>>();
            var cardsPerPile = remainingCards.Count / NumberOfDrawPiles;
            var remainder = remainingCards.Count % NumberOfDrawPiles;

            var currentIndex = 0;

            for (int i = 0; i < NumberOfDrawPiles; i++)
            {
                var pileSize = cardsPerPile + (i < remainder ? 1 : 0);
                var pile = remainingCards.Skip(currentIndex).Take(pileSize).ToList();
                piles.Add(pile);
                currentIndex += pileSize;
            }

            session.SetDrawPiles(piles);
        }

        private bool IsArchBaby(CardInGame card)
        {
            // Un Arch bebé tiene armyType "arch"
            return card.ArmyType != null && card.ArmyType.ToLower() == "arch";
        }

        private void PlaceArchOnBoard(CentralBoard board, CardInGame archCard)
        {
            if (archCard == null || string.IsNullOrWhiteSpace(archCard.IdCardGlobal))
            {
                return;
            }

            // Los Archs se colocan boca abajo en su ejército correspondiente
            // Aquí guardamos el idCardGlobal, no el objeto completo
            var army = board.GetArmyByType(archCard.ArmyType);
            if (army != null)
            {
                // Nota: CentralBoard.GetArmyByType retorna List<int> pero necesitamos string
                // Esto necesita ajuste en CentralBoard
            }
        }

        public PlayerSession SelectFirstPlayer(GameSession session)
        {
            if (session == null || !session.Players.Any())
            {
                return null;
            }

            // Seleccionar jugador aleatorio para empezar
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

            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                var value = BitConverter.ToUInt32(bytes, 0);
                return (int)(value % (uint)maxValue);
            }
        }
    }
}
