using Microsoft.EntityFrameworkCore;
using SET09102_2024_5.Data;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Repositories
{
    public class MaintenanceRepository
  : Repository<Maintenance>, IMaintenanceRepository
    {
        private readonly SensorMonitoringContext _ctx;
        public MaintenanceRepository(SensorMonitoringContext ctx) : base(ctx)
            => _ctx = ctx;

        public async Task<IEnumerable<Maintenance>> GetUpcomingAsync(TimeSpan window)
        {
            var now = DateTime.Now;
            return await _context.Maintenances
              .Include(m => m.Sensor)
              .Include(m => m.Maintainer)
              .Where(m => m.MaintenanceDate >= now
                       && m.MaintenanceDate <= now.Add(window))
              .ToListAsync();
        }

        public async Task<IEnumerable<Maintenance>> GetOverdueAsync()
        {
            var now = DateTime.Now;
            return await _context.Maintenances
              .Include(m => m.Sensor)
              .Include(m => m.Maintainer)
              .Where(m => m.MaintenanceDate < now)
              .ToListAsync();
        }
        public Task<IEnumerable<Maintenance>> GetAllWithDetailsAsync()
            => _ctx.Maintenances
                   .Include(m => m.Sensor)
                   .Include(m => m.Maintainer)
                   .AsNoTracking()
                   .ToListAsync()
                   .ContinueWith(t => (IEnumerable<Maintenance>)t.Result);
        public Task<Maintenance> GetByIdWithDetailsAsync(int id) =>
    _ctx.Maintenances
       .Include(m => m.Sensor)
       .Include(m => m.Maintainer)
       .FirstOrDefaultAsync(m => m.MaintenanceId == id);
    }
}
