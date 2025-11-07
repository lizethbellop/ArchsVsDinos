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

namespace ArchsVsDinosClient.Views
{
    /// <summary>
    /// Lógica de interacción para EditUsername.xaml
    /// </summary>
    public partial class EditUsername : Window
    {

        private readonly EditUsernameViewModel viewModel;
        public EditUsername()
        {
            InitializeComponent();
            viewModel = new EditUsernameViewModel(
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

        private async Task Click_BtnSave(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();

            viewModel.NewUsername = TxtB_NewUsername.Text;

            await viewModel.SaveEditUsername();
        }

        private bool ValidateInputs(string username)
        {
            if (ValidationHelper.IsEmpty(username) || ValidationHelper.IsWhiteSpace(username))
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
