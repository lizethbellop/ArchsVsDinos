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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ArchsVsDinosClient.Views.Instructions
{

    public partial class Page13 : Page
    {
        public Page13()
        {
            InitializeComponent();
        }

        private void Click_BtnNext(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Page14());
        }

        private void Click_BtnBack(object sender, RoutedEventArgs e)
        {
            this.NavigationService.GoBack();
        }
    }
}
