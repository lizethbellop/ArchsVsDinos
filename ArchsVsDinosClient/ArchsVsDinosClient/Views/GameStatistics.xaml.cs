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
    public partial class GameStatistics : Window
    {
        private readonly GameStatisticsViewModel viewModel;

        public GameStatistics(int matchId)
        {
            InitializeComponent();
            viewModel = new GameStatisticsViewModel(matchId);
            DataContext = viewModel;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            viewModel?.Dispose();
        }
    }
}
