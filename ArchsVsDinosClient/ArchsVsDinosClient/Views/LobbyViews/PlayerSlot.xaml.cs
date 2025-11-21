using ArchsVsDinosClient.Models;
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

namespace ArchsVsDinosClient.Views.LobbyViews
{

    public partial class PlayerSlot : UserControl
    {

        public PlayerSlot()
        {
            InitializeComponent();
            this.DataContextChanged += PlayerSlot_DataContextChanged;
        }

        private void PlayerSlot_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is SlotLobby slotData)
            {
                UpdateSlotVisuals(slotData);
                slotData.PropertyChanged += (senderSlot, propertyChangedEvent) =>
                {
                    UpdateSlotVisuals(slotData);
                };
            }
        }

        private void UpdateSlotVisuals(SlotLobby slotData)
        {
            if (string.IsNullOrWhiteSpace(slotData.Username))
            {
                Gr_Null.Visibility = Visibility.Visible;
                Gr_IsNotFriend.Visibility = Visibility.Collapsed;
                Gr_IsFriend.Visibility = Visibility.Collapsed;
            }
            else if (slotData.IsFriend)
            {
                Gr_Null.Visibility = Visibility.Collapsed;
                Gr_IsNotFriend.Visibility = Visibility.Collapsed;
                Gr_IsFriend.Visibility = Visibility.Visible;
            }
            else
            {
                Gr_Null.Visibility = Visibility.Collapsed;
                Gr_IsNotFriend.Visibility = Visibility.Visible;
                Gr_IsFriend.Visibility = Visibility.Collapsed;
            }

            Lb_Username.Content = slotData.Username;
            Lb_Nickname.Content = slotData.Nickname;

            Lb_FriendUsername.Content = slotData.Username;
            Lb_FriendNickname.Content = slotData.Nickname;
        }
    }
}
