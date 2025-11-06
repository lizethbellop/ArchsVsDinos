using ArchsVsDinosClient.Models;
using ArchsVsDinosClient.ProfileManagerService;
using ArchsVsDinosClient.Properties.Langs;
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
    /// Lógica de interacción para EditNickname.xaml
    /// </summary>
    public partial class EditNickname : Window
    {
        private readonly EditNicknameViewModel viewModel;
        public EditNickname()
        {
            InitializeComponent();
            viewModel = new EditNicknameViewModel();
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

           viewModel.NewNickname = TxtB_NewNickname.Text;

            await viewModel.SaveEditNickname();

        }

        private bool ValidateInputs(string nickname)
        {
            if (ValidationHelper.IsEmpty(nickname) || ValidationHelper.IsWhiteSpace(nickname))
            {
                return false;
            }

            return true;
        }

        private void OnRequestClose(object sender, System.EventArgs e)
        {
            new MainWindow().ShowDialog();
            this.Close();
        }

    }
}
