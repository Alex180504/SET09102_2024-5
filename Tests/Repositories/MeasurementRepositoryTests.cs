using Microsoft.EntityFrameworkCore;
using Moq;
using SET09102_2024_5.Data;
using SET09102_2024_5.Data.Repositories;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Tests.Repositories
{
    public class MeasurementRepositoryTests
    {
        private SensorMonitoringContext CreateContext(string name) =>
            new SensorMonitoringContext(
                new DbContextOptionsBuilder<SensorMonitoringContext>()
                    .UseInMemoryDatabase(name)
                    .Options);

        [Fact]
        public async Task GetSinceAsync_ReturnsOnlyNewerMeasurements()
        {
            var now = DateTime.UtcNow;
            var ctx = CreateContext(nameof(GetSinceAsync_ReturnsOnlyNewerMeasurements));

            // Seed required related entities
            var measurand = new Measurand
            {
                MeasurandId = 1,
                QuantityName = "Temperature",
                QuantityType = "Physical",
                Symbol = "T",
                Unit = "°C"
            };
            var sensor = new Sensor
            {
                SensorId = 1,
                MeasurandId = 1,
                Measurand = measurand,
                SensorType = "Thermometer",
                Status = "Active"
            };
            await ctx.Measurands.AddAsync(measurand);
            await ctx.Sensors.AddAsync(sensor);
            await ctx.SaveChangesAsync();

            // Now add measurements referring to SensorId = 1
            await ctx.Measurements.AddRangeAsync(new[]
            {
            new Measurement
            {
                MeasurementId = 1,
                Timestamp     = now.AddMinutes(-10),
                Value         = 1,
                SensorId      = 1
            },
            new Measurement
            {
                MeasurementId = 2,
                Timestamp     = now.AddHours(-1),
                Value         = 2,
                SensorId      = 1
            }
        });
            await ctx.SaveChangesAsync();

            var repo = new MeasurementRepository(ctx);
            var results = await repo.GetSinceAsync(now.AddMinutes(-30));

            Assert.Single(results);
            Assert.Equal(1, results[0].MeasurementId);

            var none = await repo.GetSinceAsync(now.AddMinutes(1));
            Assert.Empty(none);
        }

        [Fact]
        public async Task GetLatestForSensorAsync_ReturnsMostRecentDtoOrNull()
        {
            var now = DateTime.UtcNow;
            var ctx = CreateContext(nameof(GetLatestForSensorAsync_ReturnsMostRecentDtoOrNull));

            // Seed required related entities
            var measurand = new Measurand
            {
                MeasurandId = 5,
                QuantityName = "Humidity",
                QuantityType = "Physical",
                Symbol = "RH",
                Unit = "%"
            };
            var sensor = new Sensor
            {
                SensorId = 5,
                MeasurandId = 5,
                Measurand = measurand,
                SensorType = "Hygrometer",
                Status = "Active"
            };
            await ctx.Measurands.AddAsync(measurand);
            await ctx.Sensors.AddAsync(sensor);
            await ctx.SaveChangesAsync();

            // Add two measurements for sensor 5
            await ctx.Measurements.AddRangeAsync(new[]
            {
            new Measurement
            {
                MeasurementId = 1,
                Timestamp     = now.AddHours(-2),
                Value         = 10,
                SensorId      = 5
            },
            new Measurement
            {
                MeasurementId = 2,
                Timestamp     = now.AddHours(-1),
                Value         = 20,
                SensorId      = 5
            }
        });
            await ctx.SaveChangesAsync();

            var repo = new MeasurementRepository(ctx);

            var dto = await repo.GetLatestForSensorAsync(5);
            Assert.NotNull(dto);
            Assert.Equal(20, dto.Value);
            Assert.Equal(now.AddHours(-1).ToString("O"), dto.Timestamp?.ToString("O"));

            var none = await repo.GetLatestForSensorAsync(99);
            Assert.Null(none);
        }

        [Fact]
        public async Task GetSinceAsync_WithRepositoryException_Throws()
        {
            // simulate DB error by mocking context to throw when accessing Measurements
            var mockCtx = new Mock<SensorMonitoringContext>(new DbContextOptions<SensorMonitoringContext>());
            mockCtx.Setup(c => c.Measurements).Throws(new Exception("db failure"));

            var repo = new MeasurementRepository(mockCtx.Object);

            await Assert.ThrowsAsync<Exception>(async () =>
                await repo.GetSinceAsync(DateTime.UtcNow));
        }
    }
}
