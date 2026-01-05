using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ArchsVsDinosClient.Views.MatchViews
{
    public partial class MatchDiscardPile : Window
    {
        public int? SelectedCardId { get; private set; }

        public MatchDiscardPile(List<Card> discardedCards)
        {
            InitializeComponent();

            if (discardedCards == null || discardedCards.Count == 0)
            {
                IC_DiscardedCards.ItemsSource = new List<Card>();
            }
            else
            {
                IC_DiscardedCards.ItemsSource = discardedCards;
                Debug.WriteLine($"[DISCARD WINDOW] Opened with {discardedCards?.Count ?? 0} cards.");
            }
        }

        private void Click_BtnSelectCard(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int cardId)
            {
                SelectedCardId = cardId;
                DialogResult = true;
                Close();
            }
        }

        private void Click_BtnClose(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
