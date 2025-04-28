using MySql.Data.MySqlClient;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Services
{
    public class MySqlBackupService : IBackupService
    {
        private readonly string _connectionString;
        private readonly string _backupFolder;

        public MySqlBackupService(string connectionString, string backupFolder)
        {
            _connectionString = connectionString;
            _backupFolder = backupFolder;
            // Ensure the local folder exists
            Directory.CreateDirectory(_backupFolder);
        }

        public async Task BackupNowAsync()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"SensorDB_{timestamp}.sql";
            var fullPath = Path.Combine(_backupFolder, fileName);

            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand { Connection = conn };
            var mb = new MySqlBackup(cmd);

            await conn.OpenAsync();
            // Exports entire DB schema + data
            mb.ExportToFile(fullPath);
        }

        public Task<IEnumerable<BackupInfo>> ListBackupsAsync()
        {
            var files = Directory
                .EnumerateFiles(_backupFolder, "*.sql")
                .Select(f => new BackupInfo
                {
                    FileName = Path.GetFileName(f),
                    CreatedOn = File.GetCreationTime(f)
                })
                .OrderByDescending(b => b.CreatedOn);

            return Task.FromResult(files.AsEnumerable());
        }

        public async Task PruneBackupsAsync(int keepLatest)
        {
            var oldBackups = (await ListBackupsAsync()).Skip(keepLatest);
            foreach (var b in oldBackups)
                File.Delete(Path.Combine(_backupFolder, b.FileName));
        }

        public async Task RestoreAsync(string backupFile)
        {
            var fullPath = Path.Combine(_backupFolder, backupFile);

            using var conn = new MySqlConnection(_connectionString);
            using var cmd = new MySqlCommand { Connection = conn };
            var mb = new MySqlBackup(cmd);

            await conn.OpenAsync();
            // Import the dump back into the DB
            mb.ImportFromFile(fullPath);
        }
    }
}
