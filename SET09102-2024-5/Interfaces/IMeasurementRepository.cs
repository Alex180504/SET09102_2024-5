using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Data.Repositories
{
    public interface IMeasurementRepository : IRepository<Measurement>
    {
        Task<List<Measurement>> GetSinceAsync(DateTime since);
        Task<MeasurementDto?> GetLatestForSensorAsync(int sensorId);

    }
}
