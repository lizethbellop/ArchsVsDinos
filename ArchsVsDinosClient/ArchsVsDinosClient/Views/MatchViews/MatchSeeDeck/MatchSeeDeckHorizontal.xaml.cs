using System;
using System.Collections.Generic;
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
using ArchsVsDinosClient.ViewModels.GameViewsModels;

namespace ArchsVsDinosClient.Views.MatchViews.MatchSeeDeck
{
    public partial class MatchSeeDeckHorizontal : Window
    {
        public GameSeeDeckViewModel ViewModel { get; }

        public MatchSeeDeckHorizontal(GameSeeDeckViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = ViewModel;
        }

        private void Click_BtnClose(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
