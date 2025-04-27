using SET09102_2024_5.Models;

namespace SET09102_2024_5.Interfaces
{
    public interface ISensorService
    {
        // Fired whenever a sensor is updated in a polling iteration.
        event Action<Sensor, DateTime?> OnSensorUpdated;

        // Fired if an unexpected error occurs in the polling loop.
        event Action<Exception> OnError;

        // Returns all sensors with their Configuration loaded.
        Task<List<Sensor>> GetAllWithConfigurationAsync();

        // Begins polling at the given interval until the token is cancelled.
        Task StartAsync(TimeSpan pollingInterval, CancellationToken cancellationToken);
    }
}

