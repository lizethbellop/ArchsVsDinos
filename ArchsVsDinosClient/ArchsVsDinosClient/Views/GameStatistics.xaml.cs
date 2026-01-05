using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.GameService;
using ArchsVsDinosClient.ViewModels.GameViewsModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;

namespace ArchsVsDinosClient.Views
{
    public partial class GameStatistics : BaseSessionWindow
    {
        private readonly GameStatisticsViewModel viewModel;

        public GameStatistics(GameEndedDTO gameEndedData, List<LobbyPlayerDTO> players)
        {
            InitializeComponent();

            viewModel = new GameStatisticsViewModel(gameEndedData, players);
            viewModel.RequestClose += GoToMenu;
            DataContext = viewModel;
            this.ExtraCleanupAction = async () => { await Task.CompletedTask; };
        }

        private void Click_BtnExit(object sender, RoutedEventArgs e)
        {
            GoToMenu();
        }

        private void GoToMenu()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (this.IsVisible)
                {
                    this.IsNavigating = true;
                    var mainWindow = new MainWindow();
                    Application.Current.MainWindow = mainWindow;
                    mainWindow.Show();
                    this.Close();
                }
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (viewModel != null)
            {
                viewModel.RequestClose -= GoToMenu;
                viewModel.Dispose();
            }

        }
    }

}