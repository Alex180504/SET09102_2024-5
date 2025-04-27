using SET09102_2024_5.Data.Repositories;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Services
{
    public class SensorService : ISensorService
    {
        private readonly ISensorRepository _sensorRepo;

        public event Action<Sensor, DateTime?> OnSensorUpdated;

        // Raised if an exception is thrown inside the polling loop.
        public event Action<Exception> OnError;

        public SensorService(ISensorRepository sensorRepo)
        {
            _sensorRepo = sensorRepo;
        }

        public Task<List<Sensor>> GetAllWithConfigurationAsync()
           => _sensorRepo.GetAllWithConfigurationAsync();

        public async Task StartAsync(TimeSpan pollingInterval, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Treat null as empty enumeration
                    var list = await GetAllWithConfigurationAsync()
                                ?? Enumerable.Empty<Sensor>();
                    foreach (var s in list)
                        OnSensorUpdated?.Invoke(s, null);

                    await Task.Delay(pollingInterval, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // expected on cancellation—swallow
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                throw;
            }
        }
    }
}
