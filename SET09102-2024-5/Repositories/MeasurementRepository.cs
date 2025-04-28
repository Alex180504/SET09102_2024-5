using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Data.Repositories
{
    public class MeasurementRepository
        : Repository<Measurement>, IMeasurementRepository
    {
        private readonly SensorMonitoringContext _ctx;

        public MeasurementRepository(SensorMonitoringContext ctx, IMemoryCache cache)
            : base(ctx, cache) => _ctx = ctx;

        public Task<List<Measurement>> GetSinceAsync(DateTime since)
        {
            return _ctx.Measurements
                       .Include(m => m.Sensor)
                       .AsNoTracking()
                       .Where(m => m.Timestamp.HasValue && m.Timestamp.Value > since)
                       .ToListAsync();
        }
        public async Task<MeasurementDto?> GetLatestForSensorAsync(int sensorId)
        {
            return await _ctx.Measurements
                .Where(m => m.SensorId == sensorId && m.Timestamp.HasValue)
                .OrderByDescending(m => m.Timestamp)
                .Select(m => new MeasurementDto
                {
                    Value = m.Value,
                    Timestamp = m.Timestamp
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

    }
}