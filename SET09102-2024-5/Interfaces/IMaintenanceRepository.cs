using SET09102_2024_5.Data.Repositories;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Interfaces
{
    public interface IMaintenanceRepository : IRepository<Maintenance>
    {
        Task<IEnumerable<Maintenance>> GetUpcomingAsync(TimeSpan window);
        Task<IEnumerable<Maintenance>> GetOverdueAsync();
        Task<IEnumerable<Maintenance>> GetAllWithDetailsAsync();
        Task<Maintenance> GetByIdWithDetailsAsync(int id);
    }
}
