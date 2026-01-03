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

            viewModel = new GameStatisticsViewModel(gameEndedData, players);

            viewModel.RequestClose += () => this.Close();

            DataContext = viewModel;
        }

        private void Click_BtnExit(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (viewModel != null)
            {
                viewModel.Dispose();
            }

            bool isMainWindowOpen = false;

            foreach (Window window in Application.Current.Windows)
            {
                if (window is MainWindow)
                {
                    isMainWindowOpen = true;
                    window.Show();
                    break;
                }
            }

            if (!isMainWindowOpen)
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
        }
    }
}