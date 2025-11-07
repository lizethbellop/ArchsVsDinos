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

namespace ArchsVsDinosClient.Views
{
    /// <summary>
    /// Lógica de interacción para Match.xaml
    /// </summary>
    public partial class Match : Window
    {
        public Match()
        {
            InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Click_BtnChat(object sender, RoutedEventArgs e)
        {
            Gr_Chat.Visibility = Visibility.Visible;
            Btn_Chat.Visibility = Visibility.Collapsed;
        }

        private void Click_BtnClose(object sender, RoutedEventArgs e)
        {
            Gr_Chat.Visibility = Visibility.Collapsed;
            Btn_Chat.Visibility = Visibility.Visible;
        }


    }
}
