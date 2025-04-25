using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Data.Repositories
{
    public class MeasurementRepository : Repository<Measurement>, IMeasurementRepository
    {
        private readonly SensorMonitoringContext _ctx;
        public MeasurementRepository(SensorMonitoringContext ctx) : base(ctx)
            => _ctx = ctx;

        public Task<List<Measurement>> GetSinceAsync(DateTime since) =>
            _ctx.Measurements
                .Include(m => m.Measurand)
                    .ThenInclude(q => q.Sensor)
                .AsNoTracking()
                .Where(m => m.Timestamp > since)
                .ToListAsync();
    }
}
