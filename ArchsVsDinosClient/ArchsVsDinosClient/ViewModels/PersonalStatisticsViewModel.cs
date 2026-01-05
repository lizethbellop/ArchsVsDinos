using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.StatisticsService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ArchsVsDinosClient.Properties;
using ArchsVsDinosClient.Properties.Langs;
using System.Windows;

namespace ArchsVsDinosClient.ViewModels
{
    public class PersonalStatisticsViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IStatisticsServiceClient statisticsClient;
        private readonly int currentUserId;
        private bool isDisposed;

        private PlayerStatisticsDTO playerStats;
        private ObservableCollection<LeaderboardEntryDTO> leaderboard;
        private ObservableCollection<MatchHistoryDTO> matchHistory;

        private bool isLoading;
        private string errorMessage;
        private bool hasError;
        private int selectedTabIndex;

        public event PropertyChangedEventHandler PropertyChanged;

        public PersonalStatisticsViewModel(int userId)
        {
            currentUserId = userId;
            statisticsClient = new StatisticsServiceClient();

            Leaderboard = new ObservableCollection<LeaderboardEntryDTO>();
            MatchHistory = new ObservableCollection<MatchHistoryDTO>();

            statisticsClient.ConnectionError += OnConnectionError;

            RefreshCommand = new DelegateCommand(ExecuteRefreshAsync);
            CloseCommand = new DelegateCommand(ExecuteClose);

            LoadAllStatisticsAsync();
        }

        public PlayerStatisticsDTO PlayerStats
        {
            get => playerStats;
            set
            {
                playerStats = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalMatchesText));
                OnPropertyChanged(nameof(WinRateText));
                OnPropertyChanged(nameof(TotalPointsText));
                OnPropertyChanged(nameof(WinsLossesText));
            }
        }

        public ObservableCollection<LeaderboardEntryDTO> Leaderboard
        {
            get => leaderboard;
            set
            {
                leaderboard = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<MatchHistoryDTO> MatchHistory
        {
            get => matchHistory;
            set
            {
                matchHistory = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => isLoading;
            set
            {
                isLoading = value;
                OnPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get => errorMessage;
            set
            {
                errorMessage = value;
                OnPropertyChanged();
            }
        }

        public bool HasError
        {
            get => hasError;
            set
            {
                hasError = value;
                OnPropertyChanged();
            }
        }

        public int SelectedTabIndex
        {
            get => selectedTabIndex;
            set
            {
                selectedTabIndex = value;
                OnPropertyChanged();
            }
        }

        public string TotalMatchesText => PlayerStats != null
            ? string.Format(Lang.Statistics_TotalMatchesLabel, PlayerStats.TotalMatches)
            : Lang.Statistics_Loading;

        public string WinRateText => PlayerStats != null
            ? string.Format(Lang.Statistics_WinRateLabel, PlayerStats.WinRate)
            : Lang.Statistics_Loading;

        public string TotalPointsText => PlayerStats != null
            ? string.Format(Lang.Statistics_TotalPoints, PlayerStats.TotalPoints)
            : Lang.Statistics_Loading;

        public string WinsLossesText => PlayerStats != null
            ? string.Format(Lang.Statistics_WinsLosses, PlayerStats.TotalWins, PlayerStats.TotalLosses)
            : Lang.Statistics_Loading;

        public ICommand RefreshCommand { get; }
        public ICommand CloseCommand { get; }

        private async void LoadAllStatisticsAsync()
        {
            await LoadStatisticsAsync();
        }

        private async void ExecuteRefreshAsync()
        {
            await LoadStatisticsAsync();
        }

        private async Task LoadStatisticsAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            HasError = false;
            ErrorMessage = string.Empty;

            try
            {
                var stats = await statisticsClient.GetPlayerStatisticsAsync(currentUserId);

                if (!HasError)
                {
                    if (stats != null)
                    {
                        PlayerStats = stats;
                    }
                    else
                    {
                        HasError = true;
                        ErrorMessage = Lang.Statistics_StatsNotFound;
                    }
                }

                if (!HasError)
                {
                    var leaderboardData = await statisticsClient.GetLeaderboardAsync(10);
                    if (!HasError && leaderboardData != null)
                    {
                        Leaderboard.Clear();
                        foreach (var entry in leaderboardData) Leaderboard.Add(entry);
                    }
                }

                if (!HasError)
                {
                    var historyData = await statisticsClient.GetPlayerMatchHistoryAsync(currentUserId, 20);
                    if (!HasError && historyData != null)
                    {
                        MatchHistory.Clear();
                        foreach (var match in historyData.OrderByDescending(matchSelected => matchSelected.MatchDate)) MatchHistory.Add(match);
                    }
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = string.Format(Lang.Statistics_StatsLogError, ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnConnectionError(string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsLoading = false;

                HasError = true;
                ErrorMessage = $"{title}: {message}";

                MessageBox.Show(
                    $"{message}\n\n{Lang.Match_TryAgainLater}",
                    title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            });
        }

        private void ExecuteClose()
        {
            Dispose();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                if (statisticsClient != null)
                {
                    statisticsClient.ConnectionError -= OnConnectionError;
                    statisticsClient.Dispose();
                }

                Leaderboard?.Clear();
                MatchHistory?.Clear();
            }

            isDisposed = true;
        }

        private class DelegateCommand : ICommand
        {
            private readonly Action execute;
            private readonly Func<bool> canExecute;

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public DelegateCommand(Action execute, Func<bool> canExecute = null)
            {
                this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
                this.canExecute = canExecute;
            }

            public bool CanExecute(object parameter)
            {
                return canExecute == null || canExecute();
            }

            public void Execute(object parameter)
            {
                execute();
            }
        }

    }
}
