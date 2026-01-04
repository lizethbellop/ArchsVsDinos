using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.Properties.Langs;
using ArchsVsDinosClient.Services;
using ArchsVsDinosClient.Services.Interfaces;
using ArchsVsDinosClient.StatisticsService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ArchsVsDinosClient.ViewModels.GameViewsModels
{
    public class GameStatisticsViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly string matchCode;
        private readonly List<LobbyPlayerDTO> playersInfo;
        private ObservableCollection<PlayerStatItem> playerStats;

        private DispatcherTimer autoExitTimer;
        private int secondsToAutoExit = 60;

        private string errorMessage;
        private bool hasError;
        private string matchDateText;
        private string timerText;
        private string titleText;
        private bool isDisposed;

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action RequestClose;

        public GameStatisticsViewModel(string matchCode, List<LobbyPlayerDTO> playersInfo)
        {
            this.matchCode = matchCode;
            this.playersInfo = playersInfo;
            PlayerStats = new ObservableCollection<PlayerStatItem>();

            ExitCommand = new DelegateCommand(ExecuteExit);

            LoadMatchStatisticsAsync();
            StartAutoExitTimer();
        }

        public ObservableCollection<PlayerStatItem> PlayerStats
        {
            get => playerStats;
            set { playerStats = value; OnPropertyChanged(); }
        }

        public string MatchDateText
        {
            get => matchDateText;
            set { matchDateText = value; OnPropertyChanged(); }
        }

        public string TimerText
        {
            get => timerText;
            set { timerText = value; OnPropertyChanged(); }
        }

        public string TitleText
        {
            get => titleText;
            set { titleText = value; OnPropertyChanged(); }
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
                var stats = await statisticsClient.GetMatchStatisticsAsync(matchCode);

                if (stats?.PlayerStats != null)
                {
                    TitleText = Lang.Match_DinosVictoryTitle;
                    MatchDateText = $"Fecha: {stats.MatchDate:dd/MM/yyyy HH:mm}";
                    PlayerStats.Clear();

                    foreach (var stat in stats.PlayerStats)
                    {
                        var infoDelLobby = playersInfo?.FirstOrDefault(player =>
                            player.IdPlayer == stat.UserId ||
                            (player.Username != null && player.Username.Equals(stat.Username, StringComparison.OrdinalIgnoreCase)) ||
                            (player.Nickname != null && player.Nickname.Equals(stat.Username, StringComparison.OrdinalIgnoreCase)));

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
                if (statisticsClient != null) statisticsClient.Dispose();
            }
        }

        private BitmapImage LoadImageFromPath(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return null;
                string filename = System.IO.Path.GetFileName(path);
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string fullPath = Path.Combine(baseDir, "Resources", "Images", "Avatars", filename);

                if (File.Exists(fullPath))
                {
                    var image = new BitmapImage();
                    using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                    {
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();
                    }
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
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string fullPath = Path.Combine(baseDir, "Resources", "Images", "Avatars", "default_avatar_00.png");

                if (File.Exists(fullPath))
                {
                    var image = new BitmapImage();
                    using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                    {
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();
                    }
                    image.Freeze();
                    return image;
                }
                return null;
            }
            catch { return null; }
        }

        private void StartAutoExitTimer()
        {
            TimerText = $"{Lang.Match_ExitingIn} {secondsToAutoExit}s";
            autoExitTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

            autoExitTimer.Tick += (sender, args) =>
            {
                secondsToAutoExit--;
                TimerText = $"{Lang.Match_ExitingIn} {secondsToAutoExit}s";
                if (secondsToAutoExit <= 0) ExecuteExit();
            };
            autoExitTimer.Start();
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
                if (autoExitTimer != null) autoExitTimer.Stop();
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
            public event EventHandler CanExecuteChanged { add { } remove { } }
            public DelegateCommand(Action execute) { this.execute = execute; }
            public bool CanExecute(object parameter) => true;
            public void Execute(object parameter) => execute();
        }
    }
}