using ArchsVsDinosClient.DTO; 
using ArchsVsDinosClient.GameService;
using ArchsVsDinosClient.Properties.Langs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ArchsVsDinosClient.ViewModels.GameViewsModels
{
    public class GameStatisticsViewModel : INotifyPropertyChanged, IDisposable
    {
        private DispatcherTimer autoExitTimer;
        private int secondsToAutoExit = 60;

        public event Action RequestClose;
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<PlayerStatItem> PlayerStats { get; set; } = new ObservableCollection<PlayerStatItem>();

        private string titleText;
        public string TitleText
        {
            get => titleText;
            set
            {
                titleText = value;
                OnPropertyChanged(nameof(TitleText));
            }
        }

        private string matchDateText;
        public string MatchDateText
        {
            get => matchDateText;
            set
            {
                matchDateText = value;
                OnPropertyChanged(nameof(MatchDateText));
            }
        }

        private string timerText;
        public string TimerText
        {
            get => timerText;
            set
            {
                timerText = value;
                OnPropertyChanged(nameof(TimerText));
            }
        }

        public ICommand ExitCommand { get; }

        public GameStatisticsViewModel(GameEndedDTO gameEndedData, List<LobbyPlayerDTO> playersInfo)
        {
            ExitCommand = new RelayCommand(ExecuteExit);
            ProcessGameData(gameEndedData, playersInfo);
            StartAutoExitTimer();
        }

        private void ProcessGameData(GameEndedDTO gameEndedData, List<LobbyPlayerDTO> playersInfo)
        {
            MatchDateText = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            if (gameEndedData == null) return;

            if (gameEndedData.Reason == "ArchsVictory")
            {
                TitleText = Lang.Match_DefeatedByArchs;
            }
            else if (gameEndedData.Reason == "Aborted")
            {
                TitleText = Lang.Match_GameAbortedMessage;
            }
            else
            {
                TitleText = Lang.Match_DinosVictoryTitle;
            }

            if (gameEndedData.FinalScores != null)
            {
                var orderedScores = gameEndedData.FinalScores.OrderBy(player => player.Position);

                foreach (var score in orderedScores)
                {
                    double heightValue = 200;
                    string colorHex = "#A0A0A0";
                    string iconSymbol = "";

                    switch (score.Position)
                    {
                        case 1:
                            heightValue = 320;
                            colorHex = "#FFD700"; 
                            iconSymbol = "👑";    
                            break;
                        case 2:
                            heightValue = 280;
                            colorHex = "#C0C0C0"; 
                            iconSymbol = "🥈";    
                            break;
                        case 3:
                            heightValue = 240;
                            colorHex = "#CD7F32"; 
                            iconSymbol = "🥉";    
                            break;
                        default:
                            heightValue = 200;
                            iconSymbol = "";      
                            break;
                    }

                    BitmapImage playerImage = null;

                    if (playersInfo != null)
                    {
                        var playerInfo = playersInfo.FirstOrDefault(p => p.IdPlayer == score.UserId || p.Nickname == score.Username);
                        if (playerInfo != null && !string.IsNullOrEmpty(playerInfo.ProfilePicture))
                        {
                            playerImage = LoadImageFromPath(playerInfo.ProfilePicture);
                        }
                    }

                    if (playerImage == null)
                    {
                        playerImage = LoadDefaultImage();
                    }

                    PlayerStats.Add(new PlayerStatItem
                    {
                        Username = score.Username,
                        Points = score.Points,
                        Position = score.Position,
                        Height = heightValue,
                        BorderColor = colorHex,
                        Icon = iconSymbol,      
                        ProfileImage = playerImage 
                    });
                }
            }
        }

        private BitmapImage LoadImageFromPath(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return null;

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string cleanPath = path;

                if (path.Contains(";component/"))
                {
                    cleanPath = path.Split(new[] { ";component/" }, StringSplitOptions.None)[1];
                }
                else if (path.StartsWith("pack://"))
                {
                    cleanPath = path.Replace("pack://application:,,,", "").TrimStart('/');
                }

                string fullPath = Path.Combine(baseDir, cleanPath.TrimStart('/', '\\'));

                if (File.Exists(fullPath))
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
            catch
            {
                return null;
            }
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
                    image.BeginInit();
                    image.UriSource = new Uri(fullPath, UriKind.Absolute);
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private void StartAutoExitTimer()
        {
            TimerText = $"{Lang.Match_ExitingIn} {secondsToAutoExit}s";
            autoExitTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

            autoExitTimer.Tick += (sender, args) =>
            {
                secondsToAutoExit--;
                TimerText = $"{Lang.Match_ExitingIn} {secondsToAutoExit}s";

                if (secondsToAutoExit <= 0)
                {
                    ExecuteExit(null);
                }
            };
            autoExitTimer.Start();
        }

        private void ExecuteExit(object parameter)
        {
            Dispose();
            Application.Current.Dispatcher.Invoke(() =>
            {
                var currentWindow = Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(window => window.GetType().Name == "GameStatistics");
                currentWindow?.Close();
            });
        }

        public void Dispose()
        {
            if (autoExitTimer != null) autoExitTimer.Stop();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PlayerStatItem
    {
        public string Username { get; set; }
        public int Points { get; set; }
        public int Position { get; set; }
        public double Height { get; set; }
        public string BorderColor { get; set; }
        public string Icon { get; set; }        
        public object ProfileImage { get; set; }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> executeAction;
        private readonly Predicate<object> canExecutePredicate;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            executeAction = execute ?? throw new ArgumentNullException(nameof(execute));
            canExecutePredicate = canExecute;
        }

        public bool CanExecute(object parameter) => canExecutePredicate == null || canExecutePredicate(parameter);
        public void Execute(object parameter) => executeAction(parameter);
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}