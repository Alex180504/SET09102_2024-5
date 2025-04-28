using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Services
{
    public class MaintenanceService : IMaintenanceService
    {
        private readonly IMaintenanceRepository _repo;
        public MaintenanceService(IMaintenanceRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<Maintenance>> GetAllAsync()
    => (_repo as IMaintenanceRepository)!.GetAllWithDetailsAsync();


        public Task<Maintenance> GetByIdAsync(int id)
        => _repo.GetByIdWithDetailsAsync(id);

        public async Task ScheduleAsync(Maintenance m)
        {
            // validate here: m.MaintenanceDate >= DateTime.Now, sensor exists, etc.
            await _repo.AddAsync(m);
            await _repo.SaveChangesAsync();
        }

        public async Task UpdateAsync(Maintenance m)
        {
            _repo.Update(m);
            await _repo.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var m = await _repo.GetByIdAsync(id);
            if (m != null)
            {
                _repo.Remove(m);
                await _repo.SaveChangesAsync();
            }
        }

        public Task<IEnumerable<Maintenance>> GetUpcomingAsync()
          => _repo.GetUpcomingAsync(TimeSpan.FromDays(1));

        public Task<IEnumerable<Maintenance>> GetOverdueAsync()
          => _repo.GetOverdueAsync();
    }
}
