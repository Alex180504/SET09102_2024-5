namespace SET09102_2024_5.Models
{
    public class BackupOptions
    {
        public TimeSpan ScheduleTime { get; set; }
        public int KeepLatestBackups { get; set; }
        public string BackupFolder { get; set; }
    }
}
