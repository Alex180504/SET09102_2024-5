// Services/SensorService.cs (or wherever you keep it)
using System.Collections.Generic;
using System.Threading.Tasks;
using SET09102_2024_5.Data.Repositories;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Services
{
    public class SensorService
    {
        private readonly IRepository<Sensor> _sensorRepo;

        public event Action<Sensor, DateTime?>? OnSensorUpdated;

        public SensorService(IRepository<Sensor> sensorRepo)
        {
            _sensorRepo = sensorRepo;
        }

        public async Task<List<Sensor>> GetAllWithConfigurationAsync()
        {
            // assumes your repository eagerly loads Configuration via Include
            return (await _sensorRepo.GetAllAsync()).ToList();
        }

        public async Task StartAsync(TimeSpan pollingInterval)
        {
            while (true)
            {
                var list = await GetAllWithConfigurationAsync();
                foreach (var s in list)
                    OnSensorUpdated?.Invoke(s, null);
                await Task.Delay(pollingInterval);
            }
        }
    }
}
