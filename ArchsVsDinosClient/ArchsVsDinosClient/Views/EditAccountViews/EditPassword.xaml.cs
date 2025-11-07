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
    /// Lógica de interacción para EditPassword.xaml
    /// </summary>
    public partial class EditPassword : Window
    {

        private readonly EditPasswordViewModel viewModel;
        public EditPassword()
        {
            InitializeComponent();
            viewModel = new EditPasswordViewModel(
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
            viewModel.CurrentPassword = Pb_CurrentPassword.Password;
            viewModel.NewPassword = Pb_NewPasword.Password;
            await viewModel.SaveEditPassword();

        }

        private bool ValidateInputs(string currentPassword, string newPassword)
        {
            if (ValidationHelper.IsEmpty(currentPassword) || ValidationHelper.IsWhiteSpace(currentPassword) 
                || ValidationHelper.IsEmpty(newPassword) || ValidationHelper.IsWhiteSpace(newPassword))
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
