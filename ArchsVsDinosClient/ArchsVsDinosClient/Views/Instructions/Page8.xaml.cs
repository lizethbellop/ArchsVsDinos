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
    public partial class Page8 : Page
    {
        public Page8()
        {
            InitializeComponent();
        }

        private void Click_BtnNext(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Page9());
        }

        private void Click_BtnBack(object sender, RoutedEventArgs e)
        {
            this.NavigationService.GoBack();
        }
    }
}
