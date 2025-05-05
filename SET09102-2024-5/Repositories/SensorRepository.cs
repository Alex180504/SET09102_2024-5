using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Data.Repositories
{
    public class SensorRepository : Repository<Sensor>, ISensorRepository
    {
        private readonly SensorMonitoringContext _ctx;
        public SensorRepository(SensorMonitoringContext ctx, IMemoryCache cache) : base(ctx, cache)
            => _ctx = ctx;

        public Task<List<Sensor>> GetAllWithConfigurationAsync() =>
            _ctx.Sensors
                .Include(s => s.Configuration)
                .Include(s => s.Measurand)
                .AsNoTracking()
                .ToListAsync();
    }
}
