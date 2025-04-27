using SET09102_2024_5.Services;

namespace SET09102_2024_5.Tests.Services
{
    public class BackupServiceTests
    {
        private readonly string _tempFolder;
        private readonly MySqlBackupService _service;

        public BackupServiceTests()
        {
            _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempFolder);
            // Use a dummy connection string; we won't call BackupNowAsync here
            _service = new MySqlBackupService("Server=localhost;Uid=foo;Pwd=bar;", _tempFolder);
        }

        [Fact]
        public async Task ListBackupsAsync_EmptyFolder_ReturnsEmpty()
        {
            var list = await _service.ListBackupsAsync();
            Assert.Empty(list);
        }

        [Fact]
        public async Task ListAndPruneBackupsAsync_MaintainsKeepLatest()
        {
            // create fake files with timestamps
            for (int i = 0; i < 5; i++)
            {
                var file = Path.Combine(_tempFolder, $"bkp_{i}.sql");
                File.WriteAllText(file, "");
                File.SetCreationTime(file, DateTime.Now.AddMinutes(-i));
            }

            var all = (await _service.ListBackupsAsync()).ToList();
            Assert.Equal(5, all.Count);

            // prune to keep last 3
            await _service.PruneBackupsAsync(3);
            var pruned = (await _service.ListBackupsAsync()).ToList();
            Assert.Equal(3, pruned.Count);
            // ensure the most recent 3 remain
            Assert.Equal(all.Take(3).Select(b => b.FileName), pruned.Select(b => b.FileName));
        }
    }
}