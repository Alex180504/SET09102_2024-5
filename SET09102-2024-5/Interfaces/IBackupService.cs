using SET09102_2024_5.Models;

namespace SET09102_2024_5.Interfaces
{
    public interface IBackupService
    {
        Task BackupNowAsync();
        Task<IEnumerable<BackupInfo>> ListBackupsAsync();
        Task PruneBackupsAsync(int keepLatest);
        Task RestoreAsync(string backupFile);
    }
}
