using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using SET09102_2024_5.Features.HistoricalData.ViewModels;
using SET09102_2024_5.Models;
using SET09102_2024_5.Interfaces;

namespace SET09102_2024_5.Tests.ViewModels
{
    public class HistoricalDataViewModelTests
    {
        private class FakeDataService : IDataService
        {
            public Task<List<EnvironmentalDataModel>> GetHistoricalData(string category, string site)
            {
                var m = new EnvironmentalDataModel
                {
                    Timestamp = DateTime.Parse("2025-01-01 12:00:00"),
                    DataCategory = category,
                    SensorSite = site
                };
                m.Values["ParamA"] = 42.0;
                return Task.FromResult(new List<EnvironmentalDataModel> { m });
            }
        }

        [Fact]
        public void Constructor_SetsUpDefaults()
        {
            var vm = new HistoricalDataViewModel();

            Assert.Equal(new[] { "Air", "Water", "Weather" }, vm.Categories);
            Assert.Equal("Air", vm.SelectedCategory);
            Assert.NotNull(vm.DataPoints);
            Assert.Empty(vm.DataPoints);
        }

        [Fact]
        public async Task LoadHistoricalData_PullsFromServiceAndPopulates()
        {
            // Arrange
            var fake = new FakeDataService();
            var vm = new HistoricalDataViewModel(fake);

            // override defaults
            vm.SelectedCategory = "TestCat";
            vm.SelectedSensorSite = "TestSite";
            vm.SelectedParameter = "ParamA";

            // Act
            vm.LoadHistoricalData();
            await Task.Delay(50);  // give the async void time to complete

            // Assert
            Assert.Single(vm.DataPoints);
            var item = vm.DataPoints.First();
            Assert.Equal("TestCat", item.DataCategory);
            Assert.Equal("TestSite", item.SensorSite);
            Assert.Equal(42.0, item.Values["ParamA"]);
        }
    }
}
