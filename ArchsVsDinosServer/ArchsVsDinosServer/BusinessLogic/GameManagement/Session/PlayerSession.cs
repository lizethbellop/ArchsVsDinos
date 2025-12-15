using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using ArchsVsDinosServer.BusinessLogic.GameManagement.Cards;

namespace ArchsVsDinosServer.BusinessLogic.GameManagement.Session
{
    public class PlayerSession
    {
        private readonly object syncRoot = new object();

        private readonly List<CardInGame> hand = new List<CardInGame>();
        private readonly List<DinoInstance> dinos = new List<DinoInstance>();
        private int nextDinoId = 1;

        public int UserId { get; }
        public string Nickname { get; } 
        public int TurnOrder { get; set; }
        public int Points { get; set; }
        public IGameManagerCallback Callback { get; private set; }

        public IReadOnlyList<CardInGame> Hand => hand.AsReadOnly();
        public IReadOnlyList<DinoInstance> Dinos => dinos.AsReadOnly();

        public PlayerSession(int userId, string nickname, IGameManagerCallback callback)
        {
            UserId = userId;
            Nickname = nickname;
            Callback = callback;
        }

        public void AddCard(CardInGame card)
        {
            lock (syncRoot)
            {
                if (card != null)
                {
                    hand.Add(card);
                }
            }
        }

        public bool RemoveCard(CardInGame card)
        {
            lock (syncRoot)
            {
                if (card != null)
                {
                    return hand.Remove(card);
                }
                return false;
            }
        }

        public CardInGame RemoveCardById(int cardId)
        {
            lock (syncRoot)
            {
                if (cardId <= 0)
                {
                    return null;
                }

                var card = hand.FirstOrDefault(c => c.IdCard == cardId);
                if (card != null)
                {
                    hand.Remove(card);
                }

                return card;
            }
        }

        public CardInGame GetCardById(int cardId)
        {
            lock (syncRoot)
            {
                return hand.FirstOrDefault(c => c.IdCard == cardId);
            }
        }

        public void ClearHand()
        {
            lock (syncRoot)
            {
                hand.Clear();
            }
        }

        public void AddDino(DinoInstance dino)
        {
            lock (syncRoot)
            {
                if (dino != null)
                {
                    dinos.Add(dino);
                }
            }
        }

        public bool RemoveDino(DinoInstance dino)
        {
            lock (syncRoot)
            {
                if (dino != null)
                {
                    return dinos.Remove(dino);
                }
                return false;
            }
        }

        public DinoInstance GetDinoByHeadCardId(int headCardId)
        {
            lock (syncRoot)
            {
                if (headCardId <= 0)
                {
                    return null;
                }

                return dinos.FirstOrDefault(dino => dino.HeadCard?.IdCard == headCardId);
            }
        }

        public void ClearDinos()
        {
            lock (syncRoot)
            {
                dinos.Clear();
            }
        }

        public void SetCallback(IGameManagerCallback callback)
        {
            Callback = callback;
        }

        public int GetNextDinoId()
        {
            lock (syncRoot)
            {
                return nextDinoId++;
            }
        }

    }
}