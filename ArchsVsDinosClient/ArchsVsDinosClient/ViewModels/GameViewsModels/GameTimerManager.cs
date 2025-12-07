using System;
using System.ComponentModel;
using System.Windows.Threading;

namespace ArchsVsDinosClient.ViewModels.GameViewsModels
{
    public class GameTimerManager : INotifyPropertyChanged
    {
        private DispatcherTimer visualTimer;
        private TimeSpan timeRemaining;
        private string matchTimeDisplay = "20:00";

        public event PropertyChangedEventHandler PropertyChanged;

        public string MatchTimeDisplay
        {
            get => matchTimeDisplay;
            private set
            {
                matchTimeDisplay = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MatchTimeDisplay)));
            }
        }

        public GameTimerManager()
        {
            visualTimer = new DispatcherTimer();
            visualTimer.Interval = TimeSpan.FromSeconds(1);
            visualTimer.Tick += OnTimerTick;
        }

        private void OnTimerTick(object sender, EventArgs eventArgs)
        {
            if (timeRemaining.TotalSeconds > 0)
            {
                timeRemaining = timeRemaining.Add(TimeSpan.FromSeconds(-1));
                MatchTimeDisplay = timeRemaining.ToString(@"mm\:ss");
            }
            else
            {
                visualTimer.Stop();
                MatchTimeDisplay = "00:00";
            }
        }

        public void StartTimer(TimeSpan initialTime)
        {
            timeRemaining = initialTime;
            MatchTimeDisplay = timeRemaining.ToString(@"mm\:ss");
            if (!visualTimer.IsEnabled)
            {
                visualTimer.Start();
            }
        }

        public void UpdateTime(TimeSpan serverTime)
        {
            timeRemaining = serverTime;
            MatchTimeDisplay = timeRemaining.ToString(@"mm\:ss");
        }

        public void StopTimer()
        {
            visualTimer.Stop();
        }
    }
}