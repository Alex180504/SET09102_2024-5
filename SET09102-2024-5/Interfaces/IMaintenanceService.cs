using SET09102_2024_5.Models;

namespace SET09102_2024_5.Interfaces
{
    public interface IMaintenanceService
    {
        Task<IEnumerable<Maintenance>> GetAllAsync();
        Task<Maintenance> GetByIdAsync(int id);
        Task ScheduleAsync(Maintenance m);    // inserts new record
        Task UpdateAsync(Maintenance m);      // edit comments or date
        Task DeleteAsync(int id);
        Task<IEnumerable<Maintenance>> GetUpcomingAsync();
        Task<IEnumerable<Maintenance>> GetOverdueAsync();
    }
}
