using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchsVsDinosServer.BusinessLogic.Game_Manager.Cards;

namespace ArchsVsDinosServer.BusinessLogic.Game_Manager.Session
{
    public class PlayerSession
    {
        private readonly List<CardInGame> hand = new List<CardInGame>();
        private readonly List<DinoInstance> dinos = new List<DinoInstance>();

        public int UserId { get; }
        public string Username { get; }
        public int TurnOrder { get; set; }
        public int Points { get; set; }
        public IGameManagerCallback Callback { get; }

        public IReadOnlyList<CardInGame> Hand => hand.AsReadOnly();
        public IReadOnlyList<DinoInstance> Dinos => dinos.AsReadOnly();

        public PlayerSession(int userId, string username, IGameManagerCallback callback)
        {
            UserId = userId;
            Username = username;
            Callback = callback;
        }

        public void AddCard(CardInGame card)
        {
            if (card != null)
            {
                hand.Add(card);
            }
        }

        public bool RemoveCard(CardInGame card)
        {
            if (card != null)
            {
                return hand.Remove(card);
            }
            return false;
        }

        public CardInGame RemoveCardById(string cardGlobalId)
        {
            if (string.IsNullOrWhiteSpace(cardGlobalId))
            {
                return null;
            }

            var card = hand.FirstOrDefault(c => c.IdCardGlobal == cardGlobalId);
            if (card != null)
            {
                hand.Remove(card);
            }

            return card;
        }

        public void AddDino(DinoInstance dino)
        {
            if (dino != null)
            {
                dinos.Add(dino);
            }
        }

        public bool RemoveDino(DinoInstance dino)
        {
            if (dino != null)
            {
                return dinos.Remove(dino);
            }
            return false;
        }

        public DinoInstance GetDinoByHeadCardId(string headCardId)
        {
            if (string.IsNullOrWhiteSpace(headCardId))
            {
                return null;
            }

            return dinos.FirstOrDefault(d => d.HeadCard?.IdCardGlobal == headCardId);
        }

        public void ClearHand()
        {
            hand.Clear();
        }

        public void ClearDinos()
        {
            dinos.Clear();
        }
    }
}
