using System;
using System.Windows.Threading;

namespace ArchsVsDinosClient.Utils
{
    public sealed class GameConnectionTimer : IDisposable
    {
        private const int TickIntervalSeconds = 1;

        private readonly DispatcherTimer dispatcherTimer;
        private readonly Action onTimeout;
        private readonly TimeSpan timeout;

        private DateTime lastActivityUtc;
        private bool isExpired;

        public GameConnectionTimer(int timeoutSeconds, Action onTimeout)
        {
            if (timeoutSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
            }

            this.onTimeout = onTimeout ?? throw new ArgumentNullException(nameof(onTimeout));
            timeout = TimeSpan.FromSeconds(timeoutSeconds);

            lastActivityUtc = DateTime.UtcNow;

            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(TickIntervalSeconds)
            };

            dispatcherTimer.Tick += OnTick;
        }

        public void Start()
        {
            isExpired = false;
            lastActivityUtc = DateTime.UtcNow;
            dispatcherTimer.Start();
        }

        public void Stop()
        {
            dispatcherTimer.Stop();
        }

        public void Reset()
        {
            NotifyActivity();
        }

        public void NotifyActivity()
        {
            if (isExpired)
            {
                return;
            }

            lastActivityUtc = DateTime.UtcNow;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (isExpired)
            {
                return;
            }

            TimeSpan elapsed = DateTime.UtcNow - lastActivityUtc;
            if (elapsed < timeout)
            {
                return;
            }

            isExpired = true;
            dispatcherTimer.Stop();

            System.Diagnostics.Debug.WriteLine("[CONNECTION TIMER] Timeout expired - no server activity.");

            onTimeout.Invoke();
        }

        public void Dispose()
        {
            dispatcherTimer.Stop();
            dispatcherTimer.Tick -= OnTick;
        }
    }
}
