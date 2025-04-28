using Moq;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using SET09102_2024_5.Services;
using SET09102_2024_5.ViewModels;

namespace SET09102_2024_5.Tests.ViewModels
{
    public class DataStorageViewModelTests
    {
        private readonly Mock<IBackupService> _backupServiceMock = new();
        private readonly Mock<IDialogService> _dialogServiceMock = new();
        private readonly BackupOptions _options = new BackupOptions
        {
            KeepLatestBackups = 5,
            ScheduleTime = TimeSpan.Zero,
            BackupFolder = "./Backups"
        };
        private readonly SchedulerService _scheduler;
        private readonly DataStorageViewModel _viewModel;

        public DataStorageViewModelTests()
        {
            // Use the real scheduler; its timer won't fire during these short tests.
            _scheduler = new SchedulerService(_backupServiceMock.Object, _options);
            _viewModel = new DataStorageViewModel(
                _backupServiceMock.Object,
                _scheduler,
                _options,
                _dialogServiceMock.Object);
        }

        [Fact]
        public void RestoreCommand_CanExecute_ReturnsFalse_WhenNothingSelected()
        {
            Assert.False(_viewModel.RestoreCommand.CanExecute(null));
        }

        [Fact]
        public async Task BackupCommand_CallsBackup_PruneAndNotifiesSuccess()
        {
            // Arrange
            var backups = new List<BackupInfo>
            {
                new BackupInfo { FileName = "a.sql", CreatedOn = DateTime.Now }
            };
            _backupServiceMock
                .Setup(s => s.ListBackupsAsync())
                .ReturnsAsync(backups);

            // Act
            _viewModel.BackupCommand.Execute(null);
            await Task.Delay(50);   // give the async-void handler time to run

            // Assert
            _backupServiceMock.Verify(s => s.BackupNowAsync(), Times.Once);
            _backupServiceMock.Verify(s => s.PruneBackupsAsync(_options.KeepLatestBackups), Times.Once);
            Assert.Single(_viewModel.BackupFiles);
            _dialogServiceMock.Verify(d => d.DisplaySuccessAsync("Backup completed", "Success"), Times.Once);
        }

        [Fact]
        public async Task RestoreCommand_CallsRestore_AndNotifiesSuccess()
        {
            // Arrange: pick one
            _viewModel.SelectedBackup = new BackupInfo { FileName = "b.sql", CreatedOn = DateTime.Now };

            // Act
            _viewModel.RestoreCommand.Execute(null);
            await Task.Delay(50);

            // Assert
            _backupServiceMock.Verify(s => s.RestoreAsync("b.sql"), Times.Once);
            _dialogServiceMock.Verify(d => d.DisplaySuccessAsync("Database restored", "Success"), Times.Once);
        }
    }
}
