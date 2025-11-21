using ArchsVsDinosClient.Models;
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
    /// Lógica de interacción para PersonalStatistics.xaml
    /// </summary>
    public partial class PersonalStatistics : Window
    {
        private readonly PersonalStatisticsViewModel viewModel;
        public PersonalStatistics()
        {
            InitializeComponent();
            viewModel = new PersonalStatisticsViewModel(UserSession.Instance.CurrentUser.IdUser);
            DataContext = viewModel;
        }

        private void Btn_Close(object sender, RoutedEventArgs e)
        {
            SoundButton.PlayDestroyingRockSound();
            this.Close();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            viewModel?.Dispose();
            base.OnClosed(e);
        }

    }
}
