using ArchsVsDinosServer.BusinessLogic.Game_Manager.Cards;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts.DTO.Game_DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosServer.BusinessLogic.Game_Management
{
    public class CardHelper
    {
        private readonly Func<IDbContext> contextFactory;

        public CardHelper(ServiceDependencies dependencies)
        {
            if (dependencies == null)
            {
                throw new ArgumentNullException(nameof(dependencies));
            }

            this.contextFactory = dependencies.contextFactory ?? throw new ArgumentNullException(nameof(dependencies.contextFactory));
        }

        // Constructor sin parámetros para compatibilidad
        public CardHelper() : this(new ServiceDependencies())
        {
        }

        // Busca carta específica - Obtiene los datos combinados de una carta usando idCardGlobal (string)
        public (CardCharacter character, CardBody body) GetCardByGlobalId(string idCardGlobal)
        {
            if (string.IsNullOrWhiteSpace(idCardGlobal))
            {
                return (null, null);
            }

            using (var context = contextFactory())
            {
                var character = context.CardCharacter
                    .FirstOrDefault(c => c.idCardGlobal == idCardGlobal);

                var body = context.CardBody
                    .FirstOrDefault(c => c.idCardGlobal == idCardGlobal);

                return (character, body);
            }
        }

        // Carta aleatoria - Obtiene una carta aleatoria que tenga tanto character como body con el mismo idCardGlobal
        public (CardCharacter character, CardBody body) GetRandomCard()
        {
            using (var context = contextFactory())
            {
                var validGlobalIds = context.CardCharacter
                    .Select(c => c.idCardGlobal)
                    .Intersect(context.CardBody.Select(b => b.idCardGlobal))
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .ToList();

                if (!validGlobalIds.Any())
                {
                    return (null, null);
                }

                var randomId = validGlobalIds[GetSecureRandomNumber(validGlobalIds.Count)];

                var character = context.CardCharacter
                    .FirstOrDefault(c => c.idCardGlobal == randomId);

                var body = context.CardBody
                    .FirstOrDefault(c => c.idCardGlobal == randomId);

                return (character, body);
            }
        }

        // Baraja una lista de idCardGlobal (strings)
        public List<string> ShuffleCards(List<string> cardIds)
        {
            if (cardIds == null || cardIds.Count == 0)
            {
                return new List<string>();
            }

            var shuffled = new List<string>(cardIds);
            var n = shuffled.Count;

            for (var i = n - 1; i > 0; i--)
            {
                var j = GetSecureRandomNumber(i + 1);
                var temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }

            return shuffled;
        }

        // Crea un objeto CardInGame desde la base de datos usando idCardGlobal
        public CardInGame CreateCardInGame(string idCardGlobal)
        {
            if (string.IsNullOrWhiteSpace(idCardGlobal))
            {
                return null;
            }

            using (var context = contextFactory())
            {
                var cardBody = context.CardBody.FirstOrDefault(c => c.idCardGlobal == idCardGlobal);
                var cardCharacter = context.CardCharacter.FirstOrDefault(c => c.idCardGlobal == idCardGlobal);

                if (cardBody == null && cardCharacter == null)
                {
                    return null;
                }

                var totalPower = (cardBody?.power ?? 0) + (cardCharacter?.power ?? 0);

                return new CardInGame
                {
                    IdCardGlobal = idCardGlobal,
                    IdCardBody = cardBody?.idCardBody,
                    IdCardCharacter = cardCharacter?.idCardCharacter,
                    Name = cardBody?.name ?? cardCharacter?.name ?? string.Empty,
                    Type = cardBody != null ? "body" : cardCharacter?.type ?? string.Empty,
                    ArmyType = cardCharacter?.armyType ?? string.Empty,
                    Power = totalPower,
                    ImagePath = cardBody?.imagePath ?? cardCharacter?.imagePath ?? string.Empty
                };
            }
        }

        // Convierte CardInGame a DTO
        public static CardDTO ConvertToCardDTO(CardInGame card)
        {
            if (card == null)
            {
                return null;
            }

            return new CardDTO
            {
                IdCardGlobal = card.IdCardGlobal,
                IdCardBody = card.IdCardBody,
                IdCardCharacter = card.IdCardCharacter,
                Name = card.Name,
                Type = card.Type,
                ArmyType = card.ArmyType,
                Power = card.Power,
                ImagePath = card.ImagePath
            };
        }

        // Obtiene todos los idCardGlobal disponibles (strings únicos)
        public List<string> GetAllCardIds()
        {
            using (var context = contextFactory())
            {
                var bodyCardIds = context.CardBody
                    .Where(c => !string.IsNullOrWhiteSpace(c.idCardGlobal))
                    .Select(c => c.idCardGlobal);

                var characterCardIds = context.CardCharacter
                    .Where(c => !string.IsNullOrWhiteSpace(c.idCardGlobal))
                    .Select(c => c.idCardGlobal);

                var allCardIds = new HashSet<string>(bodyCardIds);
                allCardIds.UnionWith(characterCardIds);

                return allCardIds.ToList();
            }
        }

        // Obtiene idCardGlobal de cabezas de dinosaurios
        public List<string> GetDinoHeads()
        {
            using (var context = contextFactory())
            {
                return context.CardCharacter
                    .Where(c => c.type == "head" &&
                               c.armyType != "arch" &&
                               !string.IsNullOrWhiteSpace(c.idCardGlobal))
                    .Select(c => c.idCardGlobal)
                    .ToList();
            }
        }

        // Filtra por ejército - Obtiene todas las cartas de un tipo específico de ejército
        public List<string> GetCardsByArmyType(string armyType)
        {
            if (string.IsNullOrWhiteSpace(armyType))
            {
                return new List<string>();
            }

            using (var context = contextFactory())
            {
                return context.CardCharacter
                    .Where(c => c.armyType == armyType &&
                               !string.IsNullOrWhiteSpace(c.idCardGlobal))
                    .Select(c => c.idCardGlobal)
                    .Distinct()
                    .ToList();
            }
        }

        public List<CardInGame> CreateCardsInGame(List<string> cardIds)
        {
            if (cardIds == null || !cardIds.Any())
            {
                return new List<CardInGame>();
            }

            var cards = new List<CardInGame>();

            foreach (var cardId in cardIds)
            {
                var card = CreateCardInGame(cardId);
                if (card != null)
                {
                    cards.Add(card);
                }
            }

            return cards;
        }

        public static List<CardDTO> ConvertToCardDTOs(List<CardInGame> cards)
        {
            if (cards == null || !cards.Any())
            {
                return new List<CardDTO>();
            }

            return cards
                .Select(ConvertToCardDTO)
                .Where(dto => dto != null)
                .ToList();
        }

        public List<CardInGame> GetCardsByIds(List<string> cardIds)
        {
            if (cardIds == null || !cardIds.Any())
            {
                return new List<CardInGame>();
            }

            var cards = new List<CardInGame>();

            foreach (var cardId in cardIds)
            {
                var card = CreateCardInGame(cardId);
                if (card != null)
                {
                    cards.Add(card);
                }
            }

            return cards;
        }

        private static int GetSecureRandomNumber(int maxValue)
        {
            if (maxValue <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue), "El valor máximo debe ser mayor que cero.");
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
