using System.Reflection;
using Moq;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using SET09102_2024_5.Services;

namespace SET09102_2024_5.Tests.Services
{
    public class SchedulerServiceTests
    {
        [Fact]
        public void Start_EnablesTimer()
        {
            var mockBackup = new Mock<IBackupService>();
            var options = new BackupOptions { ScheduleTime = TimeSpan.FromHours(23), KeepLatestBackups = 1, BackupFolder = "/tmp" };
            var svc = new SchedulerService(mockBackup.Object, options);

            svc.Start();
            // Use reflection to get private Timer
            var timerField = typeof(SchedulerService)
                .GetField("_timer", BindingFlags.NonPublic | BindingFlags.Instance);
            var timer = (System.Timers.Timer)timerField.GetValue(svc);
            Assert.True(timer.Enabled);
        }
    }
}

