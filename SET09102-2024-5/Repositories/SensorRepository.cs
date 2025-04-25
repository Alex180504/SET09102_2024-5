using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Data.Repositories
{
    public class SensorRepository : Repository<Sensor>, ISensorRepository
    {
        private readonly SensorMonitoringContext _ctx;
        public SensorRepository(SensorMonitoringContext ctx) : base(ctx)
            => _ctx = ctx;

        public Task<List<Sensor>> GetAllWithConfigurationAsync() =>
            _ctx.Sensors
                .Include(s => s.Configuration)
                .AsNoTracking()
                .ToListAsync();
    }
}
