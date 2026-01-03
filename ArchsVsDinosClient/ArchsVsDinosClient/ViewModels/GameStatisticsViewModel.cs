using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.StatisticsService;
using ArchsVsDinosClient.ViewModels.GameViewsModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ArchsVsDinosClient.ViewModels
{
    public class GameStatisticsViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly int matchId;
        private readonly List<LobbyPlayerDTO> playersInfo; 
        private ObservableCollection<PlayerStatItem> playerStats; 
        private bool isLoading;
        private string errorMessage;
        private bool hasError;
        private string matchDateText;
        private bool isDisposed;

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action RequestClose;

        public GameStatisticsViewModel(int matchId, List<LobbyPlayerDTO> playersInfo)
        {
            this.matchId = matchId;
            this.playersInfo = playersInfo;
            PlayerStats = new ObservableCollection<PlayerStatItem>();

            ExitCommand = new DelegateCommand(ExecuteExit);

            LoadMatchStatisticsAsync();
        }

        public ObservableCollection<PlayerStatItem> PlayerStats
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
                        var infoDelLobby = playersInfo?.FirstOrDefault(player =>
                            player.IdPlayer == stat.UserId ||
                            (player.Username != null && player.Username.Equals(stat.Username, StringComparison.OrdinalIgnoreCase)));

                        BitmapImage playerImage = null;
                        if (infoDelLobby != null && !string.IsNullOrEmpty(infoDelLobby.ProfilePicture))
                        {
                            playerImage = LoadImageFromPath(infoDelLobby.ProfilePicture);
                        }
                        if (playerImage == null) playerImage = LoadDefaultImage();

                        double heightValue = 200;
                        string colorHex = "#A0A0A0";
                        string iconSymbol = "";

                        switch (stat.Position)
                        {
                            case 1: heightValue = 320; colorHex = "#FFD700"; iconSymbol = "👑"; break;
                            case 2: heightValue = 280; colorHex = "#C0C0C0"; iconSymbol = "🥈"; break;
                            case 3: heightValue = 240; colorHex = "#CD7F32"; iconSymbol = "🥉"; break;
                        }

                        PlayerStats.Add(new PlayerStatItem
                        {
                            Username = stat.Username,
                            Points = stat.Points,
                            Position = stat.Position,
                            Height = heightValue,
                            BorderColor = colorHex,
                            Icon = iconSymbol,
                            ProfileImage = playerImage
                        });
                    }

                    MatchDateText = $"Fecha: {stats.MatchDate:dd/MM/yyyy HH:mm}";
                }
                else
                {
                    MessageBox.Show(Lang.Match_Can_tSaveStatistics);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{Lang.GlobalUnexpectedError} {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (statisticsClient != null)
                {
                    statisticsClient.ConnectionError -= OnConnectionError;
                    statisticsClient.Dispose();
                }
            }
        }

        private BitmapImage LoadImageFromPath(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return null;

                string cleanPath = path;

                // 1. Limpiamos la basura de la ruta Pack URI para dejar solo "Resources/..."
                if (path.Contains(";component/"))
                {
                    var parts = path.Split(new[] { ";component/" }, StringSplitOptions.None);
                    if (parts.Length > 1) cleanPath = parts[1];
                }
                else if (path.StartsWith("pack://"))
                {
                    // Caso raro, intentamos limpiar lo básico
                    cleanPath = path.Replace("pack://application:,,,", "").TrimStart('/');
                }

                // 2. Construimos la ruta física absoluta
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string fullPath = System.IO.Path.Combine(baseDir, cleanPath.TrimStart('/', '\\'));

                // 3. Cargamos si existe
                if (System.IO.File.Exists(fullPath))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(fullPath, UriKind.Absolute);
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
                return null;
            }
            catch { return null; }
        }

        private BitmapImage LoadDefaultImage()
        {
            try
            {
                // Construimos la ruta física directa
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string fullPath = System.IO.Path.Combine(baseDir, "Resources", "Images", "Avatars", "default_avatar_00.png");

                if (System.IO.File.Exists(fullPath))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(fullPath, UriKind.Absolute);
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
                return null;
            }
            catch { return null; }
        }

        private void OnConnectionError(string title, string message)
        {
            HasError = true;
            ErrorMessage = $"{title}: {message}";
        }

        private void ExecuteExit()
        {
            RequestClose?.Invoke();
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

        public class PlayerStatItem
        {
            public string Username { get; set; }
            public int Points { get; set; }
            public int Position { get; set; }
            public double Height { get; set; }
            public string BorderColor { get; set; }
            public string Icon { get; set; }
            public BitmapImage ProfileImage { get; set; }
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

            public bool CanExecute(object parameter) => canExecute == null || canExecute();
            public void Execute(object parameter) => execute();
        }
    }
}
