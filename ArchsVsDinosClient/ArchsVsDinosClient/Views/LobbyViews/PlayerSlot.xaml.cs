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

        /*
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
        }*/

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
                    // Pasamos el pincel del grid de amigos
                    LoadPlayerAvatar(slotData, ImgFriendAvatar);
                }
                else
                {
                    Gr_IsNotFriend.Visibility = Visibility.Visible;
                    Gr_IsFriend.Visibility = Visibility.Collapsed;
                    // Pasamos el pincel del grid de no amigos
                    LoadPlayerAvatar(slotData, ImgPlayerAvatar);
                }
            }
        }

        /*
        private void LoadPlayerAvatar(SlotLobby slotData, System.Windows.Media.ImageBrush imageBrush)
        {
            try
            {
                if (!string.IsNullOrEmpty(slotData.ProfilePicture))
                {
                    imageBrush.ImageSource = new BitmapImage(new Uri($"pack://application:,,,/{slotData.ProfilePicture}", UriKind.Absolute));
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
        }*/


        // Propiedad para evitar recargas innecesarias
        private string CurrentLoadedPath { get; set; } = string.Empty;

        private void LoadPlayerAvatar(SlotLobby slotData, System.Windows.Media.ImageBrush imageBrush)
        {
            if (slotData == null || imageBrush == null) return;

            this.Dispatcher.Invoke(() => {
                try
                {
                    // --- EL CAMBIO CLAVE ---
                    // Si el slotData NO trae foto (está vacío), pero el círculo YA TIENE una imagen puesta...
                    // ¡NO HACEMOS NADA! No vamos a dejar que la 00 borre a la 05.
                    if (string.IsNullOrEmpty(slotData.ProfilePicture) && imageBrush.ImageSource != null)
                    {
                        return;
                    }

                    // Decidir ruta
                    string selectedPath = string.IsNullOrEmpty(slotData.ProfilePicture)
                        ? "/Resources/Images/Avatars/default_avatar_00.png"
                        : slotData.ProfilePicture;

                    // Limpieza y URI
                    string cleanPath = selectedPath.TrimStart('/', '\\');
                    string packUri = $"pack://application:,,,/ArchsVsDinosClient;component/{cleanPath}";

                    // Solo logueamos si es la 05 para ver si sobrevive
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

        private void SetDefaultImage(System.Windows.Media.ImageBrush imageBrush)
        {
            try
            {
                imageBrush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/ArchsVsDinosClient;component/Resources/Images/Avatars/default_avatar_00.png"));
            }
            catch { }
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
