// Services/SensorService.cs (or wherever you keep it)
using System.Collections.Generic;
using System.Threading.Tasks;
using SET09102_2024_5.Data.Repositories;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Services
{
    public class SensorService
    {
        private readonly ISensorRepository _sensorRepo;

        public event Action<Sensor, DateTime?>? OnSensorUpdated;

        public SensorService(ISensorRepository sensorRepo)
        {
            _sensorRepo = sensorRepo;
        }

        public Task<List<Sensor>> GetAllWithConfigurationAsync()
           => _sensorRepo.GetAllWithConfigurationAsync();

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
