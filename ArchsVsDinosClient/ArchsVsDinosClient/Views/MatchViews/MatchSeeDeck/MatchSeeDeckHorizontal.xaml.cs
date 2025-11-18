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

namespace ArchsVsDinosClient.Views.MatchViews.MatchSeeDeck
{
    /// <summary>
    /// Lógica de interacción para MatchSeeDeckHorizontal.xaml
    /// </summary>
    public partial class MatchSeeDeckHorizontal : Window
    {
        public MatchSeeDeckHorizontal()
        {
            InitializeComponent();
        }

        private void Click_BtnClose(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
