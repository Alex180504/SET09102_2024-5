﻿using Microsoft.EntityFrameworkCore;
using SET09102_2024_5.Data;
using SET09102_2024_5.Data.Repositories;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Tests.Repositories
{
    public class SensorRepositoryTests
    {
        private SensorMonitoringContext CreateContext(string name) =>
            new SensorMonitoringContext(
                new DbContextOptionsBuilder<SensorMonitoringContext>()
                    .UseInMemoryDatabase(name)
                    .Options);

        [Fact]
        public async Task GetAllWithConfigurationAsync_IncludesConfigAndMeasurand()
        {
            // Arrange
            var ctx = CreateContext(nameof(GetAllWithConfigurationAsync_IncludesConfigAndMeasurand));

            // Seed a measurand
            var meas = new Measurand
            {
                MeasurandId = 1,
                QuantityName = "Q",
                QuantityType = "T",
                Symbol = "S",
                Unit = "U"
            };
            await ctx.Measurands.AddAsync(meas);

            // Seed a configuration
            var cfg = new Configuration
            {
                SensorId = 42,
                Latitude = 1f,
                Longitude = 2f
            };
            await ctx.Configurations.AddAsync(cfg);

            // Seed the sensor with required non-nullable fields
            var sensor = new Sensor
            {
                SensorId = 42,
                MeasurandId = 1,
                Measurand = meas,
                Configuration = cfg,
                SensorType = "Thermometer",  // <— required
                Status = "Active"        // <— required
            };
            await ctx.Sensors.AddAsync(sensor);

            await ctx.SaveChangesAsync();

            // Act
            var repo = new SensorRepository(ctx);
            var list = await repo.GetAllWithConfigurationAsync();

            // Assert
            Assert.Single(list);
            var s = list[0];
            Assert.NotNull(s.Configuration);
            Assert.Equal(42, s.Configuration.SensorId);
            Assert.NotNull(s.Measurand);
            Assert.Equal(1, s.Measurand.MeasurandId);
        }
        [Fact]
        public async Task GetAllWithConfigurationAsync_EmptyDatabase_ReturnsEmptyList()
        {
            var ctx = CreateContext(nameof(GetAllWithConfigurationAsync_EmptyDatabase_ReturnsEmptyList));
            var repo = new SensorRepository(ctx);

            var list = await repo.GetAllWithConfigurationAsync();

            Assert.NotNull(list);
            Assert.Empty(list);
        }
        [Fact]
        public async Task
        GetAllWithConfigurationAsync_MissingConfiguration_ReturnsSensorWithNullConfig()
        {
            // Arrange
            var ctx = CreateContext(nameof(GetAllWithConfigurationAsync_MissingConfiguration_ReturnsSensorWithNullConfig));

            // 1) Seed a measurand
            var meas = new Measurand
            {
                MeasurandId = 1,
                QuantityName = "Q1",
                QuantityType = "T1",
                Symbol = "S1",
                Unit = "U1"
            };
            await ctx.Measurands.AddAsync(meas);

            // 2) Seed a sensor that points to that measurand but has no Configuration
            var sensor = new Sensor
            {
                SensorId = 42,
                MeasurandId = 1,
                SensorType = "TypeA",
                Status = "Active"
                // Configuration stays null
            };
            await ctx.Sensors.AddAsync(sensor);

            await ctx.SaveChangesAsync();

            var repo = new SensorRepository(ctx);

            // Act
            var list = await repo.GetAllWithConfigurationAsync();

            // Assert
            Assert.Single(list);
            var result = list[0];
            Assert.Equal(42, result.SensorId);
            Assert.NotNull(result.Measurand);       // Measurand loaded OK
            Assert.Null(result.Configuration);      // Configuration is null as expected
        }
    }
}
