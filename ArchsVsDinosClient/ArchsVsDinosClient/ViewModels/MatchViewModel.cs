using ArchsVsDinosClient.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.ViewModels
{/*
    public class MatchViewModel : BaseViewModel
    {
        private readonly ChatServiceClient chatService;
        private readonly string username;

        public ObservableCollection<CardModel> MyDeck { get; set; } = new ObservableCollection<CardModel>();

        public MatchViewModel(string username)
        {
            this.username = username;
            chatService = new ChatServiceClient();
            LoadMyDeck();
        }

        private void LoadMyDeck()
        {
            var cartas = GetCardsPlayer();
            foreach (var carta in cartas)
                MyDeck.Add(carta);
        }

        // Método simulado para obtener cartas
        private List<CardModel> GetCardsPlayer()
        {
            // Simulación, reemplazar con la real
            return new List<CardModel>
            {
                new CardModel { Name = "Card_Reverse", ImagePath = "/Resources/Images/Cards/Reverse/reverse.png" },
            };
        }

    }*/
}
