using System.Windows.Input;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using SET09102_2024_5.Services;

namespace SET09102_2024_5.ViewModels
{
    public class DataStorageViewModel : BaseViewModel
    {
        private readonly IBackupService _backupService;
        private readonly SchedulerService _scheduler;
        private readonly IDialogService _dialogService;
        private BackupOptions _options;

        public ICommand BackupCommand { get; }
        public ICommand SaveSettingsCommand { get; }

        public TimeSpan ScheduleTime
        {
            get => _options.ScheduleTime;
            set { _options.ScheduleTime = value; OnPropertyChanged(); }
        }

        public int KeepLatestBackups
        {
            get => _options.KeepLatestBackups;
            set { _options.KeepLatestBackups = value; OnPropertyChanged(); }
        }

        public DataStorageViewModel(
            IBackupService backupService,
            SchedulerService scheduler,
            BackupOptions options,
            IDialogService dialogService)
        {
            _backupService = backupService;
            _scheduler = scheduler;
            _options = options;
            _dialogService = dialogService;

            BackupCommand = new Command(async () =>
            {
                await _backupService.BackupNowAsync();
                await _backupService.PruneBackupsAsync(_options.KeepLatestBackups);
                await _dialogService.DisplaySuccessAsync("Backup completed");
            });

            SaveSettingsCommand = new Command(() =>
            {
                // persist options and restart scheduler
                _scheduler.Start();
                _dialogService.DisplaySuccessAsync("Changes saved");
            });
        }
    }
}
