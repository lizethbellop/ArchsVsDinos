/*using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.GameService;
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
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ArchsVsDinosClient.ViewModels.GameViewsModels
{
    public class GameStatisticsViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly GameEndedDTO directResults;
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

        private string warningMessage;
        private bool hasWarning;

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
}*/

using ArchsVsDinosClient.DTO;
using ArchsVsDinosClient.GameService;
using ArchsVsDinosClient.Properties.Langs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ArchsVsDinosClient.ViewModels.GameViewsModels
{
    public class GameStatisticsViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly GameEndedDTO gameData;
        private readonly List<LobbyPlayerDTO> playersInfo;

        private ObservableCollection<PlayerStatItem> playerStats;
        private DispatcherTimer autoExitTimer;
        private int secondsToAutoExit = 60;

        private string titleText;
        private string matchDateText;
        private string timerText;

        // Propiedades para el aviso de error
        private string warningMessage;
        private bool hasWarning;

        private bool isDisposed;

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action RequestClose;

        public GameStatisticsViewModel(GameEndedDTO gameData, List<LobbyPlayerDTO> playersInfo)
        {
            this.gameData = gameData;
            this.playersInfo = playersInfo;

            PlayerStats = new ObservableCollection<PlayerStatItem>();
            ExitCommand = new DelegateCommand(ExecuteExit);

            // Carga inmediata (Memoria RAM)
            LoadStatisticsFromMemory();

            StartAutoExitTimer();
        }

        public string WarningMessage
        {
            get => warningMessage;
            set { warningMessage = value; OnPropertyChanged(); }
        }

        public bool HasWarning
        {
            get => hasWarning;
            set { hasWarning = value; OnPropertyChanged(); }
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

        private void LoadStatisticsFromMemory()
        {
            MatchDateText = $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}";

            if (gameData.Reason == "Aborted") TitleText = Lang.Match_GameAbortedMessage;
            else if (gameData.Reason == "ArchsVictory") TitleText = Lang.Match_DefeatedByArchs;
            else TitleText = Lang.Match_DinosVictoryTitle;

            if (!gameData.IsStatsSaved)
            {
                HasWarning = true;
                WarningMessage = Lang.Match_StatsSavePending; 

                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(
                        Lang.Match_StatsSavePendingMessage,
                        Lang.GlobalServerError,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                });
            }

            PlayerStats.Clear();
            if (gameData.FinalScores != null)
            {
                var sortedScores = gameData.FinalScores.OrderByDescending(s => s.Points).ToList();
                int rank = 1;

                foreach (var score in sortedScores)
                {
                    var pInfo = playersInfo?.FirstOrDefault(p => p.IdPlayer == score.UserId);

                    double h = 200;
                    string color = "#A0A0A0";
                    string icon = "";

                    if (rank == 1) { h = 320; color = "#FFD700"; icon = "👑"; }
                    else if (rank == 2) { h = 280; color = "#C0C0C0"; icon = "🥈"; }
                    else if (rank == 3) { h = 240; color = "#CD7F32"; icon = "🥉"; }

                    BitmapImage img = null;
                    if (pInfo != null) img = LoadImageFromPath(pInfo.ProfilePicture);
                    if (img == null) img = LoadDefaultImage();

                    PlayerStats.Add(new PlayerStatItem
                    {
                        Username = score.Username,
                        Points = score.Points,
                        Position = rank,
                        Height = h,
                        BorderColor = color,
                        Icon = icon,
                        ProfileImage = img
                    });
                    rank++;
                }
            }
        }

        private BitmapImage LoadImageFromPath(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return null;
                string f = Path.GetFileName(path);
                string d = AppDomain.CurrentDomain.BaseDirectory;
                string full = Path.Combine(d, "Resources", "Images", "Avatars", f);
                if (File.Exists(full)) return new BitmapImage(new Uri(full));
                return null;
            }
            catch { return null; }
        }

        private BitmapImage LoadDefaultImage()
        {
            try
            {
                string full = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Images", "Avatars", "default_avatar_00.png");
                if (File.Exists(full)) return new BitmapImage(new Uri(full));
                return null;
            }
            catch { return null; }
        }

        private void StartAutoExitTimer()
        {
            TimerText = $"{Lang.Match_ExitingIn} {secondsToAutoExit}s";
            autoExitTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            autoExitTimer.Tick += (s, a) =>
            {
                secondsToAutoExit--;
                TimerText = $"{Lang.Match_ExitingIn} {secondsToAutoExit}s";
                if (secondsToAutoExit <= 0) ExecuteExit();
            };
            autoExitTimer.Start();
        }

        private void ExecuteExit() => RequestClose?.Invoke();

        public void Dispose() => autoExitTimer?.Stop();

        protected virtual void OnPropertyChanged([CallerMemberName] string n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

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
            private readonly Action ex;
            public event EventHandler CanExecuteChanged { add { } remove { } }
            public DelegateCommand(Action ex) => this.ex = ex;
            public bool CanExecute(object p) => true;
            public void Execute(object p) => ex();
        }
    }
}