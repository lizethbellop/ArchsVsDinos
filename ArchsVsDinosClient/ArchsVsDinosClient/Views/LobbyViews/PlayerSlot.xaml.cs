using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.ViewModels;
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

        public LobbyViewModel ViewModel
        {
            get => (LobbyViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public static readonly DependencyProperty ViewModelProperty =  DependencyProperty.Register("ViewModel", typeof(LobbyViewModel), typeof(PlayerSlot));

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
                LoadPlayerAvatar(slotData, ImgFriendAvatar);
            }
            else
            {
                Gr_Null.Visibility = Visibility.Collapsed;
                Gr_IsNotFriend.Visibility = Visibility.Visible;
                Gr_IsFriend.Visibility = Visibility.Collapsed;
                LoadPlayerAvatar(slotData, ImgPlayerAvatar);
            }
        }

        private void LoadPlayerAvatar(SlotLobby slotData, System.Windows.Media.ImageBrush imageBrush)
        {
            try
            {
                if (!string.IsNullOrEmpty(slotData.ProfilePicture))
                {
                    imageBrush.ImageSource = new BitmapImage(new Uri(slotData.ProfilePicture, UriKind.Relative));
                }
                else
                {
                    imageBrush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/Avatars/default_avatar_00.png"));
                }
            }
            catch (Exception ex)
            {
                imageBrush.ImageSource = null;
            }
        }

        
        private void Click_BtnKick(object sender, RoutedEventArgs e)
        {/*

            Button clickedButton = sender as Button;
            if (clickedButton != null)
            {
                string targetUsername = clickedButton.Tag as string;
                if (!string.IsNullOrEmpty(targetUsername))
                {
                    bool currentUserIsHost = ViewModel.CurrentClientIsHost();

                    if (currentUserIsHost)
                    {
                        MessageBoxResult confirmation = MessageBox.Show($"{Lang.Lobby_QuestKick} {targetUsername}?", Lang.GlobalAcceptText, MessageBoxButton.YesNo);

                        if (confirmation == MessageBoxResult.Yes)
                        {
                            string currentUsername = UserSession.Instance.CurrentUser.Username;

                            ViewModel.ExpelThePlayer(targetUsername, currentUsername);

                        }
                    }
                    else
                    {
                        MessageBox.Show(Lang.Lobby_OnlyHostCanKick);
                    }
                }
            }*/
        }
    }
}
