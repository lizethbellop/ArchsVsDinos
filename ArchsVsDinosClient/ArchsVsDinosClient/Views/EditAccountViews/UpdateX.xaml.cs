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
    /// Lógica de interacción para UpdateX.xaml
    /// </summary>
    public partial class UpdateX : Window
    {
        private readonly UpdateXViewModel viewModel;

        public UpdateX()
        {
            InitializeComponent();
            viewModel = new UpdateXViewModel(
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
            viewModel.NewXLink = TxtB_XLink.Text;
            await viewModel.SaveXLink();
        }

        private bool ValidateInputs(string xLink)
        {
            if (ValidationHelper.IsEmpty(xLink) || ValidationHelper.IsWhiteSpace(xLink))
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
