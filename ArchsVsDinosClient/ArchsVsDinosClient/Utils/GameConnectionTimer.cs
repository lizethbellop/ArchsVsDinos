using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ArchsVsDinosClient.Utils
{
    public class GameConnectionTimer : IDisposable
    {
        private readonly DispatcherTimer timer;
        private readonly Action onTimeout;
        private bool hasExpired = false;

        public GameConnectionTimer(int timeoutSeconds, Action onTimeout)
        {
            this.onTimeout = onTimeout;

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(timeoutSeconds)
            };

            timer.Tick += OnTimerExpired;
        }

        public void Start()
        {
            hasExpired = false;
            timer.Start();
        }

        public void Reset()
        {
            if (!hasExpired)
            {
                timer.Stop();
                timer.Start();
            }
        }

        public void Stop()
        {
            timer.Stop();
        }

        private void OnTimerExpired(object sender, EventArgs e)
        {
            if (hasExpired) return;

            hasExpired = true;
            timer.Stop();

            System.Diagnostics.Debug.WriteLine("[CONNECTION TIMER] ⚠️ No server response - timeout expired");

            onTimeout?.Invoke();
        }

        public void Dispose()
        {
            timer.Stop();
            timer.Tick -= OnTimerExpired;
        }
    }
}
