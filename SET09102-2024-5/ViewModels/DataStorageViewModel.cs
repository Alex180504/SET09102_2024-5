using System.Collections.ObjectModel;
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
        private readonly IDialogService _dialog;
        private BackupOptions _options;
        private BackupInfo _selectedBackup;

        public ObservableCollection<BackupInfo> BackupFiles { get; }
            = new ObservableCollection<BackupInfo>();

        public BackupInfo SelectedBackup
        {
            get => _selectedBackup;
            set
            {
                _selectedBackup = value;
                OnPropertyChanged();
                ((Command)RestoreCommand).ChangeCanExecute();
            }
        }

        public ICommand BackupCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand RestoreCommand { get; }

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
            IDialogService dialog)
        {
            _backupService = backupService;
            _scheduler = scheduler;
            _options = options;
            _dialog = dialog;

            BackupCommand = new Command(async () =>
            {
                await _backupService.BackupNowAsync();
                await LoadBackupsAsync();
                await _backupService.PruneBackupsAsync(_options.KeepLatestBackups);
                await _dialog.DisplaySuccessAsync("Backup completed");
            });

            SaveSettingsCommand = new Command(() =>
            {
                _scheduler.Start();
            });

            RestoreCommand = new Command(async () =>
            {
                if (SelectedBackup != null)
                {
                    await _backupService.RestoreAsync(SelectedBackup.FileName);
                    await _dialog.DisplaySuccessAsync("Database restored");
                }
            }, () => SelectedBackup != null);
        }

        public async Task LoadBackupsAsync()
        {
            BackupFiles.Clear();
            var backups = await _backupService.ListBackupsAsync();
            foreach (var b in backups)
                BackupFiles.Add(b);
        }
    }
}
