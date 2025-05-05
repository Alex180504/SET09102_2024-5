using System.Collections.Generic;
using System.Threading.Tasks;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Interfaces
{
    public interface ISensorRepository : IRepository<Sensor>
    {
        Task<List<Sensor>> GetAllWithConfigurationAsync();
    }
}
