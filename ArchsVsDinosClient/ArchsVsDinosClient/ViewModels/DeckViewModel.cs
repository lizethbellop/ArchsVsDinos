using ArchsVsDinosClient.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.ViewModels
{
    public class DeckViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Card> MyCards { get; private set; }

        public DeckViewModel()
        {
            MyCards = new ObservableCollection<Card>();
        }

        public void LoadPlayerCards(List<Card> cards)
        {
            MyCards.Clear();
            foreach (var card in cards)
                MyCards.Add(card);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

