using ArchsVsDinosClient.Utils;
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
    /// Lógica de interacción para Lobby.xaml
    /// </summary>
    public partial class Lobby : Window
    {
        public Lobby()
        {
            InitializeComponent();
        }


        private void Btn_CancelMatch(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            this.Close();
        }

        private void Btn_InviteFriends(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            Gr_MyFriends.Visibility = Visibility.Visible;
        }

        private void Btn_CancelInviteFriend(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            Gr_MyFriends.Visibility = Visibility.Collapsed;
        }

        private void Btn_InvitePlayerByEmail(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            Gr_InviteByEmail.Visibility = Visibility.Visible;
        }

        private void Btn_CancelInviteByEmail(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            Gr_InviteByEmail.Visibility = Visibility.Collapsed;
        }


    }
}
