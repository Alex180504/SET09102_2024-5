using System.Reflection;
using Microsoft.Extensions.Logging;
using Moq;
using SET09102_2024_5.Data.Repositories;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using SET09102_2024_5.ViewModels;

namespace SET09102_2024_5.Tests.ViewModels
{
    public class MapViewModelTests
    {
        private readonly Mock<IMainThreadService> _mainThread = new();
        private readonly Mock<IMeasurementRepository> _measurementRepo = new();
        private readonly Mock<IDialogService> _dialog = new();
        private readonly Mock<ILogger<MapViewModel>> _logger = new();
        private readonly Mock<ISensorService> _sensorService = new();

        private MapViewModel CreateVm()
        {
            // Mock ISensorService so VM can subscribe without hitting real polling
            _sensorService
                        .Setup(s => s.StartAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);
            _sensorService
                        .Setup(s => s.GetAllWithConfigurationAsync())
                        .ReturnsAsync(new List<Sensor>());

            return new MapViewModel(
                _sensorService.Object,
                _mainThread.Object,
                _measurementRepo.Object,
                _dialog.Object,
                _logger.Object);
        }

        private object InvokePrivate(MethodInfo mi, object instance, params object[] args)
            => mi.Invoke(instance, args);

        [Fact]
        public void FormatTimeSpan_ProducesReadableString()
        {
            var vm = CreateVm();
            var mi = typeof(MapViewModel).GetMethod("FormatTimeSpan", BindingFlags.NonPublic | BindingFlags.Instance);

            var result1 = (string)InvokePrivate(mi, vm, TimeSpan.FromMinutes(75));
            Assert.Contains("1 hour", result1);
            Assert.Contains("15 minute", result1);

            var result0 = (string)InvokePrivate(mi, vm, TimeSpan.Zero);
            Assert.Equal("0 seconds", result0);
        }

        [Fact]
        public void GetWarningReason_ReturnsNullForInactiveOrMaintenance()
        {
            var vm = CreateVm();
            var mi = typeof(MapViewModel).GetMethod("GetWarningReason", BindingFlags.NonPublic | BindingFlags.Instance);
            var sensor = new Sensor { Status = "Inactive", Configuration = new Configuration { MeasurementFrequency = 1 } };

            var r = InvokePrivate(mi, vm, sensor, null);
            Assert.Null(r);

            sensor.Status = "Maintenance";
            r = InvokePrivate(mi, vm, sensor, null);
            Assert.Null(r);
        }

        [Fact]
        public void GetWarningReason_NoData_YieldsNoRecentData()
        {
            var vm = CreateVm();
            var mi = typeof(MapViewModel).GetMethod("GetWarningReason", BindingFlags.NonPublic | BindingFlags.Instance);
            var sensor = new Sensor { Status = "Active", Configuration = new Configuration { MeasurementFrequency = 5 } };

            var r = (string)InvokePrivate(mi, vm, sensor, null);
            Assert.Equal("no recent data", r);
        }

        [Fact]
        public void GetWarningReason_StaleReading_YieldsLateMessage()
        {
            var vm = CreateVm();
            var mi = typeof(MapViewModel).GetMethod("GetWarningReason", BindingFlags.NonPublic | BindingFlags.Instance);
            var cfg = new Configuration { MeasurementFrequency = 1 };
            var sensor = new Sensor { Status = "Active", Configuration = cfg };
            var last = new MeasurementDto { Timestamp = DateTime.UtcNow.AddMinutes(-5) }; // 4 min late

            var r = (string)InvokePrivate(mi, vm, sensor, last);
            Assert.Contains("late by", r);
        }

        [Fact]
        public void GetWarningReason_ThresholdBreaches_YieldsBelowOrAbove()
        {
            var vm = CreateVm();
            var mi = typeof(MapViewModel).GetMethod("GetWarningReason", BindingFlags.NonPublic | BindingFlags.Instance);
            var cfg = new Configuration { MeasurementFrequency = 0, MinThreshold = 10, MaxThreshold = 20 };
            var meas = new MeasurementDto { Value = 5, Timestamp = DateTime.UtcNow };
            var sensor = new Sensor { Status = "Active", Configuration = cfg, Measurand = new Measurand { Unit = "U" } };

            var below = (string)InvokePrivate(mi, vm, sensor, meas);
            Assert.Contains("below minimum", below);
            Assert.Contains("U", below);

            meas.Value = 25;
            var above = (string)InvokePrivate(mi, vm, sensor, meas);
            Assert.Contains("above maximum", above);
        }
    }
}
