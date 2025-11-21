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

namespace ArchsVsDinosClient.ViewModels
{
    public class GameStatisticsViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly int matchId;
        private ObservableCollection<PlayerMatchStatsDTO> playerStats;
        private bool isLoading;
        private string errorMessage;
        private bool hasError;
        private string matchDateText;
        private bool isDisposed;

        public event PropertyChangedEventHandler PropertyChanged;

        public GameStatisticsViewModel(int matchId)
        {
            this.matchId = matchId;
            PlayerStats = new ObservableCollection<PlayerMatchStatsDTO>();

            ExitCommand = new DelegateCommand(ExecuteExit);

            LoadMatchStatisticsAsync();
        }

        public ObservableCollection<PlayerMatchStatsDTO> PlayerStats
        {
            get => playerStats;
            set
            {
                playerStats = value;
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

        public string MatchDateText
        {
            get => matchDateText;
            set
            {
                matchDateText = value;
                OnPropertyChanged();
            }
        }

        public ICommand ExitCommand { get; }

        private async void LoadMatchStatisticsAsync()
        {
            await LoadStatisticsAsync();
        }

        private async Task LoadStatisticsAsync()
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = string.Empty;

            IStatisticsServiceClient statisticsClient = null;

            try
            {
                statisticsClient = new StatisticsServiceClient();
                statisticsClient.ConnectionError += OnConnectionError;

                var stats = await statisticsClient.GetMatchStatisticsAsync(matchId);

                if (stats?.PlayerStats != null)
                {
                    PlayerStats.Clear();

                    foreach (var stat in stats.PlayerStats)
                    {
                        PlayerStats.Add(stat);
                    }

                    MatchDateText = $"Fecha: {stats.MatchDate:dd/MM/yyyy HH:mm}";
                }
                else
                {
                    HasError = true;
                    ErrorMessage = "No se pudieron cargar las estadísticas de la partida";
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Error al cargar las estadísticas: {ex.Message}";
            }
            finally
            {
                if (statisticsClient != null)
                {
                    statisticsClient.ConnectionError -= OnConnectionError;
                    statisticsClient.Dispose();
                }
                IsLoading = false;
            }
        }

        private void OnConnectionError(string title, string message)
        {
            HasError = true;
            ErrorMessage = $"{title}: {message}";
        }

        private void ExecuteExit()
        {
            // Cerrar la ventana y volver al menú principal
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
                PlayerStats?.Clear();
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
