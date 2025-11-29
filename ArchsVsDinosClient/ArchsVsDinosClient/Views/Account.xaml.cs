using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Utils;
using ArchsVsDinosClient.Views;
using ArchsVsDinosClient.Views.EditAccountViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
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
    /// Lógica de interacción para Account.xaml
    /// </summary>
    public partial class Account : Window
    {
        public Account()
        {
            InitializeComponent();
            LoadUserData();
            UserProfileObserver.Instance.OnProfileUpdated += RefreshUserData;
        }

        private async void LoadUserData()
        {
            var user = UserSession.Instance.CurrentUser;
            TxtUsername.Text = user.Username;
            TxtNickname.Text = user.Nickname;
            TxtEmail.Text = user.Email;
            TxtName.Text = user.Name;

            await LoadUserAvatar();
        }

        private async Task LoadUserAvatar()
        {
            try
            {
                var profileService = new ProfileServiceClient();
                string avatarPath = await profileService.GetProfilePictureAsync(UserSession.Instance.CurrentUser.Username);

                if (!string.IsNullOrEmpty(avatarPath))
                {
                    ImgAvatar.Source = new BitmapImage(new Uri(avatarPath, UriKind.Relative));
                }
            }
            catch (CommunicationException ex)
            {
                Console.WriteLine($"Error de comunicación al obtener avatar: {ex.Message}");
                ImgAvatar.Source = new BitmapImage(new Uri("/Resources/Images/Avatars/default_avatar_01.png", UriKind.Relative));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar avatar: {ex.Message}");
                ImgAvatar.Source = new BitmapImage(new Uri("/Resources/Images/Avatars/default_avatar_01.png", UriKind.Relative));
            }
        }

        private void RefreshUserData()
        {
            LoadUserData();
        }

        protected override void OnClosed(EventArgs e)
        {
            UserProfileObserver.Instance.OnProfileUpdated -= RefreshUserData;
            base.OnClosed(e);
        }

        private void Click_BtnCancel(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            this.Close();   
        }

        private void Click_BtnPersonalStatistics(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new PersonalStatistics().ShowDialog();
        }

        private void Click_BtnEditPassword(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new EditPassword().ShowDialog();
        }

        private void Click_BtnEditUsername(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new EditUsername().ShowDialog();
        }

        private void Click_BtnEditNickname(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new EditAccountViews.EditNickname().ShowDialog();
        }

        private void Click_BtnEditAvatar(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            new AvatarSelection().ShowDialog();
        }

        private void Click_BtnFacebook(object sender, RoutedEventArgs e)
        {
            string facebookUrl = UserSession.Instance.CurrentPlayer.Facebook;
            SocialMediaHelper.TryOpenSocialMediaLink(facebookUrl, SocialMediaPlatform.Facebook);
        }

        private void Click_BtnInstagram(object sender, RoutedEventArgs e)
        {
            string instagramUrl = UserSession.Instance.CurrentPlayer.Instagram;
            SocialMediaHelper.TryOpenSocialMediaLink(instagramUrl, SocialMediaPlatform.Instagram);
        }

        private void Click_BtnX(object sender, RoutedEventArgs e)
        {
            string xUrl = UserSession.Instance.CurrentPlayer.X;
            SocialMediaHelper.TryOpenSocialMediaLink(xUrl, SocialMediaPlatform.X);
        }

        private void Click_BtnTiktok(object sender, RoutedEventArgs e)
        {
            string tiktokUrl = UserSession.Instance.CurrentPlayer.Tiktok;
            SocialMediaHelper.TryOpenSocialMediaLink(tiktokUrl, SocialMediaPlatform.TikTok);

        }

        private void Click_BtnEditFacebook(object sender, RoutedEventArgs e)
        {
            new UpdateFacebook().ShowDialog();
        }

        private void Click_BtnEditInstagram(object sender, RoutedEventArgs e)
        {
            new UpdateInstagram().ShowDialog();
        }

        private void Click_BtnEditX(object sender, RoutedEventArgs e)
        {
            new UpdateX().ShowDialog();
        }

        private void Click_BtnEditTiktok(object sender, RoutedEventArgs e)
        {
            new UpdateTikTok().ShowDialog();

        }

    }
}
