using Microsoft.EntityFrameworkCore;
using Moq;
using SET09102_2024_5.Data;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using SET09102_2024_5.Tests.Mocks;
using SET09102_2024_5.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tests.Mocks;
using Xunit;

namespace SET09102_2024_5.Tests.ViewModels
{
    public class SensorManagementViewModelTests
    {
        private readonly SensorMonitoringContext _mockContext;
        private readonly List<Sensor> _testSensors;
        private readonly DbContextOptions<SensorMonitoringContext> _options;
        private readonly MockMainThreadService _mockMainThreadService;
        private readonly MockDialogService _mockDialogService;

        public SensorManagementViewModelTests()
        {
            // Create test data
            _testSensors = new List<Sensor>
            {
                new Sensor
                {
                    SensorId = 1,
                    SensorType = "Temperature",
                    Status = "Active",
                    Measurand = new Measurand
                    {
                        MeasurandId = 1,
                        QuantityName = "Temperature",
                        QuantityType = "Physical",
                        Symbol = "T",
                        Unit = "°C"
                    },
                    DisplayName = "Temperature Sensor 1"
                },
                new Sensor
                {
                    SensorId = 2,
                    SensorType = "Humidity",
                    Status = "Inactive",
                    Measurand = new Measurand
                    {
                        MeasurandId = 2,
                        QuantityName = "Humidity",
                        QuantityType = "Physical",
                        Symbol = "RH",
                        Unit = "%"
                    },
                    DisplayName = "Humidity Sensor 1"
                }
            };

            // Create in-memory database for testing
            _options = new DbContextOptionsBuilder<SensorMonitoringContext>()
                .UseInMemoryDatabase(databaseName: "TestSensorDb_" + Guid.NewGuid().ToString())
                .Options;

            // Create and set up the context
            _mockContext = new MockSensorMonitoringContext(_options, _testSensors);

            // Initialize mock services
            _mockMainThreadService = new MockMainThreadService();
            _mockDialogService = new MockDialogService();
        }

        private SensorManagementViewModel CreateViewModel()
        {
            return new SensorManagementViewModel(
                _mockContext,
                _mockMainThreadService,
                _mockDialogService);
        }


        // Test class for mocking the DbContext using MockDbSetFactory
        private class MockSensorMonitoringContext : SensorMonitoringContext
        {
            private readonly List<Sensor> _sensors;

            public MockSensorMonitoringContext(DbContextOptions<SensorMonitoringContext> options, List<Sensor> sensors)
                : base(options)
            {
                _sensors = sensors;
            }

            public override DbSet<Sensor> Sensors => MockDbSetFactory.CreateMockDbSetWithAdvancedOperations<Sensor>(_sensors, "SensorId");

            public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                return await Task.FromResult(1); // Mock successful save
            }
        }

        [Fact]
        public void Constructor_InitializesProperties()
        {
            // Arrange & Act
            var viewModel = CreateViewModel();

            // Assert
            Assert.NotNull(viewModel.Sensors);
            Assert.NotNull(viewModel.FilteredSensors);
            Assert.NotNull(viewModel.LoadSensorsCommand);
            Assert.NotNull(viewModel.SaveChangesCommand);
            Assert.NotNull(viewModel.SearchCommand);
            Assert.NotNull(viewModel.ClearSearchCommand);
            Assert.NotNull(viewModel.ValidateCommand);
            Assert.False(viewModel.IsLoading);
            Assert.False(viewModel.IsSensorSelected);
            Assert.False(viewModel.HasValidationErrors);
            Assert.NotNull(viewModel.StatusOptions);
            Assert.Equal(4, viewModel.StatusOptions.Count); // "Active", "Inactive", "Maintenance", "Error"
        }

        [Fact]
        public void Constructor_WithNullContext_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SensorManagementViewModel(null));
        }


        [Fact]
        public void FilterSensors_WithEmptySearch_ShowsAllSensors()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.Sensors.Clear();
            foreach (var sensor in _testSensors)
            {
                viewModel.Sensors.Add(sensor);
            }

            // Act
            viewModel.FilterSensors("");

            // Assert
            Assert.True(viewModel.IsSearchActive);
            Assert.Equal(_testSensors.Count, viewModel.FilteredSensors.Count);
        }

        [Fact]
        public void FilterSensors_WithValidSearch_FiltersCorrectly()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.Sensors.Clear();
            foreach (var sensor in _testSensors)
            {
                viewModel.Sensors.Add(sensor);
            }

            // Act - filter by SensorType
            viewModel.FilterSensors("Temperature");

            // Assert
            Assert.True(viewModel.IsSearchActive);
            Assert.Single(viewModel.FilteredSensors);
            Assert.Equal("Temperature", viewModel.FilteredSensors[0].SensorType);
        }

        [Fact]
        public void FilterSensors_WithMeasurandSearch_FiltersCorrectly()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.Sensors.Clear();
            foreach (var sensor in _testSensors)
            {
                viewModel.Sensors.Add(sensor);
            }

            // Act - filter by Measurand
            viewModel.FilterSensors("Humidity");

            // Assert
            Assert.True(viewModel.IsSearchActive);
            Assert.Single(viewModel.FilteredSensors);
            Assert.Equal("Humidity", viewModel.FilteredSensors[0].Measurand.QuantityName);
        }

        [Fact]
        public void FilterSensors_WithNoMatches_ReturnsEmptyList()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.Sensors.Clear();
            foreach (var sensor in _testSensors)
            {
                viewModel.Sensors.Add(sensor);
            }

            // Act
            viewModel.FilterSensors("NoSuchSensor");

            // Assert
            Assert.True(viewModel.IsSearchActive);
            Assert.Empty(viewModel.FilteredSensors);
        }

        [Fact]
        public void FilterSensors_WithCaseInsensitiveSearch_FiltersCorrectly()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.Sensors.Clear();
            foreach (var sensor in _testSensors)
            {
                viewModel.Sensors.Add(sensor);
            }

            // Act - search with different case
            viewModel.FilterSensors("tEmPeRaTuRe");

            // Assert
            Assert.True(viewModel.IsSearchActive);
            Assert.Single(viewModel.FilteredSensors);
            Assert.Equal("Temperature", viewModel.FilteredSensors[0].SensorType);
        }

        [Fact]
        public void HideSearchResults_SetsIsSearchingToFalse()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.IsSearchActive = true;

            // Act
            viewModel.HideSearchResults();

            // Assert
            Assert.False(viewModel.IsSearchActive);
        }

        [Fact]
        public void ShowAllSensorsInSearch_ShowsAllSensorsInFilteredSensors()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.Sensors.Clear();
            foreach (var sensor in _testSensors)
            {
                viewModel.Sensors.Add(sensor);
            }

            // Act
            viewModel.ShowAllSensorsInSearch();

            // Assert
            Assert.True(viewModel.IsSearchActive);
            Assert.Equal(_testSensors.Count, viewModel.FilteredSensors.Count);
        }

        [Fact]
        public void OrientationText_Get_ReturnsEmptyStringWhenConfigurationIsNull()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.Configuration = null;

            // Act
            var result = viewModel.OrientationText;

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void OrientationText_Get_ReturnsEmptyStringWhenOrientationIsNull()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.Configuration = new Configuration
            {
                Orientation = null
            };

            // Act
            var result = viewModel.OrientationText;

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void OrientationText_Get_ReturnsOrientationAsString()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.Configuration = new Configuration
            {
                Orientation = 90
            };

            // Act
            var result = viewModel.OrientationText;

            // Assert
            Assert.Equal("90", result);
        }

        [Fact]
        public void OrientationText_Set_DoesNothingWhenConfigurationIsNull()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.Configuration = null;

            // Act
            viewModel.OrientationText = "90";

            // Assert
            Assert.Null(viewModel.Configuration);
        }

        [Fact]
        public void OrientationText_Set_SetsNullWhenValueIsEmpty()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.Configuration = new Configuration
            {
                Orientation = 90
            };

            // Act
            viewModel.OrientationText = "";

            // Assert
            Assert.Null(viewModel.Configuration.Orientation);
        }

        [Fact]
        public void OrientationText_Set_SetsOrientationFromNumericString()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.Configuration = new Configuration
            {
                Orientation = 0
            };

            // Act
            viewModel.OrientationText = "90";

            // Assert
            Assert.Equal(90, viewModel.Configuration.Orientation);
        }

        [Fact]
        public void OrientationText_Set_SetsOrientationFromStringWithDegreeSymbol()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.Configuration = new Configuration
            {
                Orientation = 0
            };

            // Act
            viewModel.OrientationText = "90°";

            // Assert
            Assert.Equal(90, viewModel.Configuration.Orientation);
        }

        [Fact]
        public void OrientationText_Set_DoesNotChangeOrientationForNonNumericString()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.Configuration = new Configuration
            {
                Orientation = 90
            };

            // Act
            viewModel.OrientationText = "not a number";

            // Assert
            Assert.Equal(90, viewModel.Configuration.Orientation);
        }

        [Fact]
        public void IsLoading_WhenSet_UpdatesCommandCanExecute()
        {
            // Arrange
            var viewModel = CreateViewModel();
            bool initialLoadSensorsCanExecute = viewModel.LoadSensorsCommand.CanExecute(null);
            bool initialSaveChangesCanExecute = viewModel.SaveChangesCommand.CanExecute(null);

            // Act
            viewModel.IsLoading = true;

            // Assert
            Assert.True(initialLoadSensorsCanExecute);
            Assert.False(viewModel.LoadSensorsCommand.CanExecute(null));
            Assert.False(viewModel.SaveChangesCommand.CanExecute(null));
        }

        [Fact]
        public void HasValidationErrors_WhenSet_UpdatesSaveChangesCommandCanExecute()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.SelectedSensor = _testSensors[0];  // Need a selected sensor
            viewModel.IsLoading = false;                 // Not loading

            bool initialCanExecute = viewModel.SaveChangesCommand.CanExecute(null);

            // Act
            viewModel.HasValidationErrors = true;

            // Assert
            Assert.False(viewModel.SaveChangesCommand.CanExecute(null));
        }

        [Fact]
        public async Task LoadSensorsCommand_ShouldLoadSensorsWhenExecuted()
        {
            // Arrange
            using var context = new SensorMonitoringContext(_options);

            // Add test sensors to the mock DB
            await context.AddRangeAsync(_testSensors); 
            await context.SaveChangesAsync();

            var viewModel = new SensorManagementViewModel(
                context,
                _mockMainThreadService,
                _mockDialogService);

            viewModel.Sensors.Clear();

            // Act
            viewModel.LoadSensorsCommand.Execute(null);
            await Task.Delay(100);

            // Assert
            Assert.Equal(_testSensors.Count, viewModel.Sensors.Count);
        }

        [Fact]
        public async Task LoadSensorsAsync_WhenIsLoadingIsTrue_DoesNotLoadSensors()
        {
            // Arrange
            using var context = new SensorMonitoringContext(_options);  // Use a real in-memory database to avoid exceptions

            await context.AddRangeAsync(_testSensors);
            await context.SaveChangesAsync();

            var viewModel = new SensorManagementViewModel(
                context,
                _mockMainThreadService,
                _mockDialogService);

            viewModel.Sensors.Clear();
            viewModel.IsLoading = true;

            // Act
            var method = typeof(SensorManagementViewModel).GetMethod(
                "LoadSensorsAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var task = (Task)method.Invoke(viewModel, null);
            await task;

            // Assert
            Assert.Empty(viewModel.Sensors); 
            Assert.True(viewModel.IsLoading); 
        }



    }
}
