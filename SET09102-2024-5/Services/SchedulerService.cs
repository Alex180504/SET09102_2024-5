using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Services
{
    public class SchedulerService
    {
        private readonly IBackupService _backupService;
        private readonly BackupOptions _options;
        private System.Timers.Timer _timer;

        public SchedulerService(IBackupService backupService, BackupOptions options)
        {
            _backupService = backupService;
            _options = options;
        }

        public void Start()
        {
            var now = DateTime.Now;
            var nextRun = DateTime.Today.Add(_options.ScheduleTime);
            if (nextRun <= now)
                nextRun = nextRun.AddDays(1);

            var initialInterval = (nextRun - now).TotalMilliseconds;

            _timer = new System.Timers.Timer(initialInterval)
            {
                AutoReset = false
            };
            _timer.Elapsed += async (sender, args) =>
            {
                try
                {
                    await _backupService.BackupNowAsync();
                    await _backupService.PruneBackupsAsync(_options.KeepLatestBackups);
                }
                catch (Exception ex)
                {
                    // handle or log exception
                }

                // Schedule daily interval thereafter
                _timer.Interval = TimeSpan.FromDays(1).TotalMilliseconds;
                _timer.AutoReset = true;
                _timer.Start();
            };

            _timer.Start();
        }
    }
}
