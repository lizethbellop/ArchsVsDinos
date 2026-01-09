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

namespace ArchsVsDinosClient.Views.Instructions
{
    public partial class Instructions : Window
    {
        public Instructions()
        {
            InitializeComponent();
            MainFrame.Navigate(new Page1());
        }

        private void Click_BtnClose(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
