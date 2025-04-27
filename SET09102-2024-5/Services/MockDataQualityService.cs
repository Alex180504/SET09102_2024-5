using Microsoft.EntityFrameworkCore;
using SET09102_2024_5.Data;
using SET09102_2024_5.Models;
using SET09102_2024_5.Interfaces;

namespace SET09102_2024_5.Services
{
    public class MockDataQualityService : IDataQualityService
    {
        private readonly SensorMonitoringContext _db;
        public MockDataQualityService(SensorMonitoringContext db) => _db = db;

        public async Task<QualityReport> RunChecksAsync(string category, string site, DateTime from, DateTime to)
        {
            var report = new QualityReport();
            var measurements = await _db.Measurements
                .Where(m => m.Timestamp >= from && m.Timestamp <= to)
                .Include(m => m.Sensor).ThenInclude(s => s.Configuration)
                .Include(m => m.Sensor).ThenInclude(s => s.Measurand)
                .ToListAsync();
            report.TotalRecords = measurements.Count;

            // Missing-data detection...
            // Out-of-range detection...
            // Duplicate detection...
            // (Paste in the logic we sketched earlier.)

            return report;
        }
    }
}
