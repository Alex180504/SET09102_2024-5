// Data/Repositories/MeasurementRepository.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Data.Repositories
{
    public class MeasurementRepository
        : Repository<Measurement>, IMeasurementRepository
    {
        private readonly SensorMonitoringContext _ctx;

        public MeasurementRepository(SensorMonitoringContext ctx)
            : base(ctx) => _ctx = ctx;

        public Task<List<Measurement>> GetSinceAsync(DateTime since)
        {
            return _ctx.Measurements
                       // If you need Sensor info, include it; otherwise you can omit Include altogether
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