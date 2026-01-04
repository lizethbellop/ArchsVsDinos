using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.GameService;
using ArchsVsDinosClient.ViewModels.GameViewsModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ArchsVsDinosClient.Views
{
    public partial class GameStatistics : Window
    {
        private readonly GameStatisticsViewModel viewModel;

        public GameStatistics(GameEndedDTO gameEndedData, List<LobbyPlayerDTO> players)
        {
            InitializeComponent();

            viewModel = new GameStatisticsViewModel(gameEndedData.MatchCode, players);

            viewModel.RequestClose += GoToMenu;

            DataContext = viewModel;
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
                    var mainWindow = new MainWindow();
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