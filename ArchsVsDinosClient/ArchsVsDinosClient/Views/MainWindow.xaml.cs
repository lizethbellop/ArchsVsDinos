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
using ArchsVsDinosClient.Models;

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
            ConfigureForTypeSession();
        }

        private void ConfigureForTypeSession()
        {
            if (UserSession.Instance.isGuest)
            {
                Btn_Creatematch.Visibility = Visibility.Collapsed;
                Btn_Friends.Visibility = Visibility.Collapsed;
                Btn_Account.Visibility = Visibility.Collapsed;
            }
        }

        private void Click_BtnLogOut(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            new Login().ShowDialog();
            this.Close();
        }

        private void Click_BtnCreateMatch(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new Lobby().ShowDialog();
        }

        private void Click_BtnAccount(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new Account().ShowDialog();
        }

        private void Click_BtnFriends(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new FriendsMainMenu().ShowDialog();
        }

        private void Click_BtnSettings(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new Settings().ShowDialog();
        }

    }
}
