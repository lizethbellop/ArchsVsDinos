using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.RegisterService;
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
using UserAccountDTO = ArchsVsDinosClient.DTO.UserAccountDTO;

namespace ArchsVsDinosClient.Views
{
    /// <summary>
    /// Lógica de interacción para Register.xaml
    /// </summary>
    public partial class Register : Window
    {

        private readonly RegisterViewModel viewModel;

        public Register()
        {

            InitializeComponent();
            viewModel = new RegisterViewModel();
            DataContext = viewModel;
        }

        private async void Click_BtnRegisterNow(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayMovingRockSound();

            viewModel.Name = TxtB_Name.Text;
            viewModel.Username = TxtB_Username.Text;
            viewModel.Email = TxtB_Email.Text;
            viewModel.Password = TxtB_Password.Text;
            viewModel.Nickname = TxtB_Nickname.Text;

            LoadingDisplayHelper.ShowLoading(LoadingOverlay);
            BtnRegister.IsEnabled = false;
            BtnCancel.IsEnabled = false;

            try
            {
                await viewModel.RegisterAsync();
            }
            finally
            {
                LoadingDisplayHelper.HideLoading(LoadingOverlay);
                BtnRegister.IsEnabled = true;
                BtnCancel.IsEnabled = true;
            }
        }

        private void Click_BtnCancel(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            MessageBox.Show(Lang.Register_CancelledRegistration);
            this.Close();
        }

        private void OnRequestClose(object sender, System.EventArgs e)
        {
            this.Close();
        }

    }
}

