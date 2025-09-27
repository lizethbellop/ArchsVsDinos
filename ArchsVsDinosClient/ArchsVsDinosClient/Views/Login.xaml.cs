using ArchsVsDinosClient.Properties.Langs;
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
using System.Windows.Shapes;

namespace ArchsVsDinosClient
{
    /// <summary>
    /// Lógica de interacción para Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        private void Btn_Register(object sender, RoutedEventArgs e)
        {
            new Register().ShowDialog();

        }
        private void Btn_PlayAsGuest(object sender, RoutedEventArgs e)
        {
            new MainWindow().ShowDialog();
            this.Close();
        }


        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbLanguage.SelectedIndex == 0)
            {
                Properties.Settings.Default.languageCode = "es-MX";

            }
            else
            {
                Properties.Settings.Default.languageCode = "en-US";
            }
            Properties.Settings.Default.Save();
        }

        private void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().ShowDialog();
        }


    }
}
