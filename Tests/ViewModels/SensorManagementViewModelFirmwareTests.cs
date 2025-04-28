using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SET09102_2024_5.Data;
using SET09102_2024_5.Models;
using SET09102_2024_5.ViewModels;
using Xunit;

namespace SET09102_2024_5.Tests.ViewModels
{
    public class SensorManagementViewModelFirmwareTests
    {
        private SensorMonitoringContext GetContext()
        {
            var options = new DbContextOptionsBuilder<SensorMonitoringContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new SensorMonitoringContext(options);
        }

        [Fact]
        public void ValidateField_EmptyFirmwareVersion_ProducesValidationError()
        {
            // arrange
            var ctx = GetContext();
            var vm = new SensorManagementViewModel(ctx);
            vm.SelectedSensor = new Sensor { SensorId = 1, Measurand = new Measurand() };
            vm.FirmwareVersion = "";
            vm.LastUpdateDate = DateTime.Today;

            // act
            vm.ValidateCommand.Execute(nameof(vm.FirmwareVersion));

            // assert
            Assert.True(vm.HasValidationErrors);
            Assert.Contains(nameof(vm.FirmwareVersion), vm.ValidationErrors.Keys);
        }

        [Fact]
        public void ValidateField_FutureLastUpdateDate_ProducesValidationError()
        {
            // arrange
            var ctx = GetContext();
            var vm = new SensorManagementViewModel(ctx);
            vm.SelectedSensor = new Sensor { SensorId = 1, Measurand = new Measurand() };
            vm.FirmwareVersion = "1.0.0";
            vm.LastUpdateDate = DateTime.Today.AddDays(1);

            // act
            vm.ValidateCommand.Execute(nameof(vm.LastUpdateDate));

            // assert
            Assert.True(vm.HasValidationErrors);
            Assert.Contains(nameof(vm.LastUpdateDate), vm.ValidationErrors.Keys);
        }

        [Fact]
        public async Task SaveChangesAsync_ValidFirmware_UpdatesDatabase()
        {
            // arrange
            var ctx = GetContext();
            var sensor = new Sensor { SensorId = 42, Measurand = new Measurand() };
            ctx.Sensors.Add(sensor);
            await ctx.SaveChangesAsync();

            var vm = new SensorManagementViewModel(ctx);
            vm.SelectedSensor = sensor;
            vm.FirmwareVersion = "2.1.5";
            vm.LastUpdateDate = DateTime.Today.AddDays(-1);

            // act
            await ((Task)typeof(SensorManagementViewModel)
                 .GetMethod("SaveChangesAsync", BindingFlags.NonPublic | BindingFlags.Instance)
                 .Invoke(vm, null));


            // assert
            var updated = await ctx.Sensors.Include(s => s.Firmware).FirstAsync(s => s.SensorId == 42);
            Assert.Equal("2.1.5", updated.Firmware.FirmwareVersion);
            Assert.Equal(vm.LastUpdateDate, updated.Firmware.LastUpdateDate);
        }
    }
}
