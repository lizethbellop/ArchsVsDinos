using System;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Threading;

namespace ArchsVsDinosClient.ViewModels.GameViewsModels
{
    public class GameTimerManager : INotifyPropertyChanged
    {
        private DispatcherTimer visualTimer;

        private DateTime targetMatchEndTime;
        private DateTime targetTurnEndTime;

        private string matchTimeDisplayText = "20:00";
        private string turnTimeDisplayText = "60";
        private SolidColorBrush turnTimeDisplayBrush = new SolidColorBrush(Colors.White);

        public event PropertyChangedEventHandler PropertyChanged;

        public string MatchTimeDisplay
        {
            get => matchTimeDisplayText;
            private set
            {
                if (matchTimeDisplayText != value)
                {
                    matchTimeDisplayText = value;
                    OnPropertyChanged(nameof(MatchTimeDisplay));
                }
            }
        }

        public string TurnTimeDisplay
        {
            get => turnTimeDisplayText;
            private set
            {
                if (turnTimeDisplayText != value)
                {
                    turnTimeDisplayText = value;
                    OnPropertyChanged(nameof(TurnTimeDisplay));
                }
            }
        }

        public SolidColorBrush TurnTimeColor
        {
            get => turnTimeDisplayBrush;
            private set
            {
                if (turnTimeDisplayBrush.Color != value.Color)
                {
                    turnTimeDisplayBrush = value;
                    OnPropertyChanged(nameof(TurnTimeColor));
                }
            }
        }

        public GameTimerManager()
        {
            visualTimer = new DispatcherTimer();
            visualTimer.Interval = TimeSpan.FromSeconds(0.2);
            visualTimer.Tick += OnVisualTimerTick;
            visualTimer.Start();
        }

        private void OnVisualTimerTick(object sender, EventArgs eventArgs)
        {
            var currentUtcTime = DateTime.UtcNow;

            var timeRemainingInMatch = targetMatchEndTime - currentUtcTime;
            if (timeRemainingInMatch.TotalSeconds < 0)
            {
                timeRemainingInMatch = TimeSpan.Zero;
            }

            MatchTimeDisplay = timeRemainingInMatch.ToString(@"mm\:ss");

            var timeRemainingInTurn = targetTurnEndTime - currentUtcTime;
            if (timeRemainingInTurn.TotalSeconds < 0)
            {
                timeRemainingInTurn = TimeSpan.Zero;
            }

            TurnTimeDisplay = ((int)timeRemainingInTurn.TotalSeconds).ToString();

            if (timeRemainingInTurn.TotalSeconds <= 10 && timeRemainingInTurn.TotalSeconds > 0)
            {
                TurnTimeColor = new SolidColorBrush(Colors.Red);
            }
            else
            {
                TurnTimeColor = new SolidColorBrush(Colors.White);
            }
        }

        public void SetMatchEndTime(DateTime serverMatchEndTimeUtc)
        {
            targetMatchEndTime = serverMatchEndTimeUtc;
            OnVisualTimerTick(null, null);
        }

        public void ResetTurnTimer(DateTime serverTurnEndTimeUtc)
        {
            targetTurnEndTime = serverTurnEndTimeUtc;
            TurnTimeColor = new SolidColorBrush(Colors.White);
            OnVisualTimerTick(null, null);
        }

        public void StopTimer()
        {
            visualTimer.Stop();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}