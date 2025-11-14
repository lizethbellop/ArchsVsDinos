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
            hand.Add(card);
        }

        public void RemoveCard(CardInGame card)
        {
            hand.Remove(card);
        }

        public void AddDino(DinoInstance dino)
        {
            dinos.Add(dino);
        }
    }
}
