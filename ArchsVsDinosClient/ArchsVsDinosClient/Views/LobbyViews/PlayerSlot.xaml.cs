using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            else
            {
                Gr_Null.Visibility = Visibility.Collapsed;

                if (slotData.IsFriend)
                {
                    Gr_IsNotFriend.Visibility = Visibility.Collapsed;
                    Gr_IsFriend.Visibility = Visibility.Visible;
                    LoadPlayerAvatar(slotData, ImgFriendAvatar);
                }
                else
                {
                    Gr_IsNotFriend.Visibility = Visibility.Visible;
                    Gr_IsFriend.Visibility = Visibility.Collapsed;
                    LoadPlayerAvatar(slotData, ImgPlayerAvatar);
                }
            }
        }

        private void LoadPlayerAvatar(SlotLobby slotData, System.Windows.Media.ImageBrush imageBrush)
        {
            if (slotData == null || imageBrush == null) return;

            this.Dispatcher.Invoke(() => {
                try
                {
                    if (string.IsNullOrEmpty(slotData.ProfilePicture) && imageBrush.ImageSource != null)
                    {
                        return;
                    }

                    string selectedPath = string.IsNullOrEmpty(slotData.ProfilePicture)
                        ? "/Resources/Images/Avatars/default_avatar_00.png"
                        : slotData.ProfilePicture;

                    string cleanPath = selectedPath.TrimStart('/', '\\');
                    string packUri = $"pack://application:,,,/ArchsVsDinosClient;component/{cleanPath}";

                    if (cleanPath.Contains("_05"))
                    {
                        Debug.WriteLine($"[AVATAR RESISTENTE] Dibujando la 05: {packUri}");
                    }

                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(packUri, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    imageBrush.ImageSource = bitmap;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AVATAR ERROR]: {ex.Message}");
                }
            });
        }

        private void Click_BtnAddFriend(object sender, RoutedEventArgs e)
        {
            var slotData = this.DataContext as SlotLobby;

            if (slotData != null && !string.IsNullOrEmpty(slotData.Username))
            {
                var window = Window.GetWindow(this);
                if (window != null && window.DataContext is LobbyViewModel viewModel)
                {
                    viewModel.SendFriendRequest(slotData.Username);
                }
            }
        }

        private void Click_BtnKick(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;

            if (clickedButton != null && clickedButton.Tag is string targetNickname && !string.IsNullOrEmpty(targetNickname))
            {
                MessageBoxResult confirmation = MessageBox.Show(
                    string.Format(Lang.Lobby_QuestKick + " {0}?", targetNickname), 
                    Lang.GlobalAcceptText,
                    MessageBoxButton.YesNo);

                if (confirmation == MessageBoxResult.Yes)
                {
                    if (ViewModel != null)
                    {
                        ViewModel.KickPlayer(targetNickname);
                    }
                }
            }

        }
    }
}
