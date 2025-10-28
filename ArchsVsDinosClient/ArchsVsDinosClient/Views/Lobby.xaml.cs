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
    public partial class Lobby : Window
    {
        public Lobby()
        {
            InitializeComponent();
        }


        private void Click_BtnCancelMatch(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            this.Close();
        }

        private void Click_BtnInviteFriends(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            Gr_MyFriends.Visibility = Visibility.Visible;
        }

        private void Click_BtnCancelInviteFriend(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            Gr_MyFriends.Visibility = Visibility.Collapsed;
        }

        private void Click_BtnInvitePlayerByEmail(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            Gr_InviteByEmail.Visibility = Visibility.Visible;
        }

        private void Click_BtnCancelInviteByEmail(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            Gr_InviteByEmail.Visibility = Visibility.Collapsed;
        }


    }
}
