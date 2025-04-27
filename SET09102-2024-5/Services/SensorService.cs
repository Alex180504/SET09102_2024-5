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

        public async Task StartAsync(TimeSpan pollingInterval, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var list = await GetAllWithConfigurationAsync();
                foreach (var s in list)
                    OnSensorUpdated?.Invoke(s, null);

                // Pass the token so Delay will end early if cancellation is requested
                await Task.Delay(pollingInterval, cancellationToken);
            }
        }
    }
}
