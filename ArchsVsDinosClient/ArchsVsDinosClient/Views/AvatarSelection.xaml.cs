using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Utils;
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
using System.Windows.Shapes;

namespace ArchsVsDinosClient.Views
{
    /// <summary>
    /// Lógica de interacción para AvatarSelection.xaml
    /// </summary>
    public partial class AvatarSelection : Window
    {
        private readonly AvatarSelectionViewModel viewModel;

        public AvatarSelection()
        {
            InitializeComponent();

            viewModel = new AvatarSelectionViewModel(
                new ProfileServiceClient(),
                new MessageService()
            );

            viewModel.RequestClose += OnRequestClose;
        }

        private void Click_BtnAvatar1(object sender, RoutedEventArgs e)
        {
            viewModel.SelectAvatar(1);
        }

        private void Click_BtnAvatar2(object sender, RoutedEventArgs e)
        {
            viewModel.SelectAvatar(2);
        }

        private void Click_BtnAvatar3(object sender, RoutedEventArgs e)
        {
            viewModel.SelectAvatar(3);
        }

        private void Click_BtnAvatar4(object sender, RoutedEventArgs e)
        {
            viewModel.SelectAvatar(4);
        }

        private void Click_BtnAvatar5(object sender, RoutedEventArgs e)
        {
            viewModel.SelectAvatar(5);
        }

        private async void Click_BtnConfirm(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            await viewModel.SaveSelectedAvatar();
        }

        private void Click_BtnCancel(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            DialogResult = false;
            Close();
        }

        private void OnRequestClose(object sender, EventArgs e)
        {
            DialogResult = true;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            viewModel.RequestClose -= OnRequestClose;
            base.OnClosed(e);
        }
    }
}
