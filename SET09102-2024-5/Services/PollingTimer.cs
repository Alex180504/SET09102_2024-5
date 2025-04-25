using System;
using System.Threading;
using System.Threading.Tasks;

namespace SET09102_2024_5.Services
{
    public class PollingTimer : IDisposable
    {
        private Timer? _timer;
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(5);
        public event Action? OnTick;

        public Task StartAsync()
        {
            // fire immediately then every Interval
            _timer = new Timer(_ => OnTick?.Invoke(), null, TimeSpan.Zero, Interval);
            return Task.CompletedTask;
        }

        public void Stop() => _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        public void Dispose() => _timer?.Dispose();
    }
}
