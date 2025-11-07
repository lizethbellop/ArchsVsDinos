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
    /// Lógica de interacción para UpdateFacebook.xaml
    /// </summary>
    public partial class UpdateFacebook : Window
    {
        private readonly UpdateFacebookViewModel viewModel;

        public UpdateFacebook()
        {
            InitializeComponent();
            viewModel = new UpdateFacebookViewModel(
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
            viewModel.NewFacebookLink = TxtB_FacebookLink.Text;
            await viewModel.SaveFacebookLink();
        }

        private bool ValidateInputs(string facebookLink)
        {
            if (ValidationHelper.IsEmpty(facebookLink) || ValidationHelper.IsWhiteSpace(facebookLink))
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
