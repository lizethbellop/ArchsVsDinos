using ArchsVsDinosClient.Views;
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
using System.Media;
using ArchsVsDinosClient.Utils;

namespace ArchsVsDinosClient
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Btn_Account(object sender, RoutedEventArgs e)
        {
            new Account().ShowDialog();
        }

        private void Btn_Friends(object sender, RoutedEventArgs e)
        {
            new FriendsMainMenu().ShowDialog();
        }

        private void Btn_Settings(object sender, RoutedEventArgs e)
        {
            new Settings().ShowDialog();
        }

        /*private void Btn_MouseEnter(object sender, MouseEventArgs e)
        {
            SoundButton sb = new SoundButton();
            sb.playButtonSound();
        }*/
    }
}
