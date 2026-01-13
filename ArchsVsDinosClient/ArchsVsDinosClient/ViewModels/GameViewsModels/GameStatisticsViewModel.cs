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