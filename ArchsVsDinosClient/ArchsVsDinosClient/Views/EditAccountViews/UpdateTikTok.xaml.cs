using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.ProfileManagerService;
using ArchsVsDinosClient.Properties.Langs;
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

namespace ArchsVsDinosClient.Views.EditAccountViews
{
    /// <summary>
    /// Lógica de interacción para UpdateTikTok.xaml
    /// </summary>
    public partial class UpdateTikTok : Window
    {
        private readonly UpdateTikTokViewModel viewModel;

        public UpdateTikTok()
        {
            InitializeComponent();
            viewModel = new UpdateTikTokViewModel(
                new ProfileServiceClient(),
                new MessageService()
            );
            DataContext = viewModel;
            viewModel.RequestClose += OnRequestClose;
        }

        private void Click_BtnCancel(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            this.Close();
        }

        private async void Click_BtnSave(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();
            viewModel.NewTikTokLink = TxtB_TikTokLink.Text;
            await viewModel.SaveTikTokLink();
        }

        private bool ValidateInputs(string tiktokLink)
        {
            if (ValidationHelper.IsEmpty(tiktokLink) || ValidationHelper.IsWhiteSpace(tiktokLink))
            {
                return false;
            }

            return true;
        }

        private void OnRequestClose(object sender, System.EventArgs e)
        {
            this.Close();
        }
    }
}
