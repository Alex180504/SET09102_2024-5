using Microsoft.EntityFrameworkCore;
using SET09102_2024_5.Data;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using SET09102_2024_5.Tests.Mocks;
using SET09102_2024_5.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SET09102_2024_5.Tests.ViewModels
{
    public class SensorOperationalStatusViewModelTests
    {
        private readonly List<Sensor> _testSensors;
        private readonly List<Incident> _testIncidents;
        private readonly List<Measurement> _testMeasurements;
        private readonly List<IncidentMeasurement> _testIncidentMeasurements;
        private readonly MockMainThreadService _mockMainThreadService;
        private readonly MockDialogService _mockDialogService;
        private readonly MockNavigationService _mockNavigationService;

        public SensorOperationalStatusViewModelTests()
        {
            // Create test sensors
            _testSensors = new List<Sensor>
            {
                new Sensor
                {
                    SensorId = 1,
                    SensorType = "Temperature",
                    Status = "Active",
                    DeploymentDate = DateTime.Now.AddDays(-30),
                    Measurand = new Measurand
                    {
                        MeasurandId = 1,
                        QuantityName = "Temperature",
                        QuantityType = "Physical",
                        Symbol = "T",
                        Unit = "°C" 
                    }
                },
                new Sensor
                {
                    SensorId = 2,
                    SensorType = "Humidity",
                    Status = "Inactive",
                    DeploymentDate = DateTime.Now.AddDays(-60),
                    Measurand = new Measurand
                    {
                        MeasurandId = 2,
                        QuantityName = "Humidity",
                        QuantityType = "Physical",
                        Symbol = "RH",
                        Unit = "%"   
                    }
                }
            };

            // Create test incidents
            _testIncidents = new List<Incident>
            {
                new Incident
                {
                    IncidentId = 1,
                    Priority = "High",
                    ResolvedDate = null,
                    ResponderComments = "Investigating",
                    IncidentMeasurements = new List<IncidentMeasurement>()
                },
                new Incident
                {
                    IncidentId = 2,
                    Priority = "Medium",
                    ResolvedDate = DateTime.Now.AddDays(-5),
                    ResponderComments = "Fixed",
                    IncidentMeasurements = new List<IncidentMeasurement>()
                }
            };

            // Create test measurements
            _testMeasurements = new List<Measurement>
            {
                new Measurement
                {
                    MeasurementId = 1,
                    SensorId = 1,
                    Value = 25.5f,
                    Timestamp = DateTime.Now.AddDays(-10),
                    IncidentMeasurements = new List<IncidentMeasurement>()
                },
                new Measurement
                {
                    MeasurementId = 2,
                    SensorId = 1,
                    Value = 30.0f,
                    Timestamp = DateTime.Now.AddDays(-9),
                    IncidentMeasurements = new List<IncidentMeasurement>()
                },
                new Measurement
                {
                    MeasurementId = 3,
                    SensorId = 2,
                    Value = 45.0f,
                    Timestamp = DateTime.Now.AddDays(-8),
                    IncidentMeasurements = new List<IncidentMeasurement>()
                }
            };

            // Create test incident measurements to connect measurements to incidents
            _testIncidentMeasurements = new List<IncidentMeasurement>
            {
                new IncidentMeasurement
                {
                    MeasurementId = 1,
                    IncidentId = 1,
                    Measurement = _testMeasurements[0],
                    Incident = _testIncidents[0]
                },
                new IncidentMeasurement
                {
                    MeasurementId = 2,
                    IncidentId = 1,
                    Measurement = _testMeasurements[1],
                    Incident = _testIncidents[0]
                },
                new IncidentMeasurement
                {
                    MeasurementId = 3,
                    IncidentId = 2,
                    Measurement = _testMeasurements[2],
                    Incident = _testIncidents[1]
                }
            };

            // Add incident measurements to both the measurements and incidents
            _testMeasurements[0].IncidentMeasurements.Add(_testIncidentMeasurements[0]);
            _testMeasurements[1].IncidentMeasurements.Add(_testIncidentMeasurements[1]);
            _testMeasurements[2].IncidentMeasurements.Add(_testIncidentMeasurements[2]);

            _testIncidents[0].IncidentMeasurements.Add(_testIncidentMeasurements[0]);
            _testIncidents[0].IncidentMeasurements.Add(_testIncidentMeasurements[1]);
            _testIncidents[1].IncidentMeasurements.Add(_testIncidentMeasurements[2]);

            // Init mock services
            _mockMainThreadService = new MockMainThreadService();
            _mockDialogService = new MockDialogService();
            _mockNavigationService = new MockNavigationService();
        }

        private SensorMonitoringContext CreateDbContext(string dbName = null)
        {
            // Create a unique database name if none provided
            dbName ??= $"TestSensorStatusDb_{Guid.NewGuid()}";

            var options = new DbContextOptionsBuilder<SensorMonitoringContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var context = new SensorMonitoringContext(options);

            // Clear database to ensure a clean state
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            return context;
        }

        private async Task<SensorOperationalStatusViewModel> CreateViewModelWithDataAsync(string dbName = null)
        {
            var context = CreateDbContext(dbName);

            // Add test data
            await context.Sensors.AddRangeAsync(_testSensors);
            await context.Incidents.AddRangeAsync(_testIncidents);
            await context.Measurements.AddRangeAsync(_testMeasurements);
            await context.IncidentMeasurements.AddRangeAsync(_testIncidentMeasurements);
            await context.SaveChangesAsync();

            return new SensorOperationalStatusViewModel(
                context,
                _mockMainThreadService,
                _mockDialogService,
                _mockNavigationService);
        }

        private SensorOperationalStatusViewModel CreateViewModel(string dbName = null)
        {
            var context = CreateDbContext(dbName);

            return new SensorOperationalStatusViewModel(
                context,
                _mockMainThreadService,
                _mockDialogService,
                _mockNavigationService);
        }

        [Fact]
        public void Constructor_InitializesProperties()
        {
            // Arrange & Act
            var viewModel = CreateViewModel();

            // Assert
            Assert.NotNull(viewModel.Sensors);
            Assert.NotNull(viewModel.LoadSensorsCommand);
            Assert.NotNull(viewModel.ApplyCommand);
            Assert.NotNull(viewModel.ViewIncidentLogCommand);
            Assert.NotNull(viewModel.SortCommand);
            Assert.Equal("", viewModel.SortProperty);
            Assert.Equal("", viewModel.SortIndicator);
            Assert.True(viewModel.IsSortAscending);
            Assert.False(viewModel.IsLoading);
            Assert.NotNull(viewModel.FilterProperties);
            Assert.Equal(5, viewModel.FilterProperties.Count); // "All", "ID", "Type", "Status", "Measurand"
            Assert.Equal("All", viewModel.SelectedFilterProperty);
        }

        [Fact]
        public void Constructor_WithNullContext_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new SensorOperationalStatusViewModel(
                    null,
                    _mockMainThreadService,
                    _mockDialogService,
                    _mockNavigationService));
        }

        [Fact]
        public async Task LoadSensorsCommand_ShouldLoadSensorsWhenExecuted()
        {
            // Arrange
            var dbName = $"TestSensorDb_{Guid.NewGuid()}";
            var context = CreateDbContext(dbName);

            // Add test sensors to the in-memory DB
            await context.Sensors.AddRangeAsync(_testSensors);
            await context.SaveChangesAsync();

            var viewModel = new SensorOperationalStatusViewModel(
                context,
                _mockMainThreadService,
                _mockDialogService,
                _mockNavigationService);

            // Initialize an empty collection to ensure we're starting from scratch
            viewModel.Sensors.Clear();

            // Act
            viewModel.LoadSensorsCommand.Execute(null);
            await Task.Delay(100);

            // Assert
            Assert.Equal(_testSensors.Count, viewModel.Sensors.Count);
        }

        [Fact]
        public async Task Sensors_WhenSet_UpdatesHasSensorsAndHasNoSensors()
        {
            // Arrange
            var viewModel = CreateViewModel();

            // Act - set null collection
            viewModel.Sensors = null;

            // Assert
            Assert.False(viewModel.HasSensors);
            Assert.True(viewModel.HasNoSensors);

            // Act - set empty collection
            viewModel.Sensors = new ObservableCollection<SensorOperationalModel>();

            // Assert
            Assert.False(viewModel.HasSensors);
            Assert.True(viewModel.HasNoSensors);

            // Act - add an item
            viewModel.Sensors.Add(new SensorOperationalModel { Id = 1 });

            // Assert
            Assert.True(viewModel.HasSensors);
            Assert.False(viewModel.HasNoSensors);
        }

        [Fact]
        public void IsLoading_WhenSet_UpdatesCommandCanExecute()
        {
            // Arrange
            var viewModel = CreateViewModel();
            bool initialLoadSensorsCanExecute = viewModel.LoadSensorsCommand.CanExecute(null);
            bool initialApplyCanExecute = viewModel.ApplyCommand.CanExecute(null);

            // Act
            viewModel.IsLoading = true;

            // Assert
            Assert.True(initialLoadSensorsCanExecute);
            Assert.True(initialApplyCanExecute);
            Assert.False(viewModel.LoadSensorsCommand.CanExecute(null));
            Assert.False(viewModel.ApplyCommand.CanExecute(null));
        }

        [Fact]
        public void SelectedSensor_WhenSet_UpdatesViewIncidentLogCommandCanExecute()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.Sensors.Add(new SensorOperationalModel { Id = 1 });

            // Check initial state
            Assert.False(viewModel.ViewIncidentLogCommand.CanExecute(null));

            // Act - set selected sensor
            var sensor = new SensorOperationalModel { Id = 1 };
            viewModel.SelectedSensor = sensor;

            // Assert
            Assert.True(viewModel.ViewIncidentLogCommand.CanExecute(sensor));
        }

        [Fact]
        public async Task ApplyFilterAndRefresh_WithEmptyFilter_ReloadsAllSensors()
        {
            // Arrange
            var viewModel = await CreateViewModelWithDataAsync();
            viewModel.FilterText = "";

            // Clear sensors to verify reload
            viewModel.Sensors.Clear();

            // Act - invoke the method via the command
            viewModel.ApplyCommand.Execute(null);
            await Task.Delay(100);

            // Assert
            Assert.Equal(_testSensors.Count, viewModel.Sensors.Count);
        }

        [Fact]
        public void SortSensors_WithEmptyPropertyName_DoesNotSort()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.Sensors.Add(new SensorOperationalModel { Id = 1 });
            viewModel.Sensors.Add(new SensorOperationalModel { Id = 2 });
            var initialOrder = viewModel.Sensors.ToList();

            // Act
            viewModel.SortCommand.Execute("");

            // Assert - order should be unchanged
            Assert.Equal(initialOrder, viewModel.Sensors);
            Assert.Equal("", viewModel.SortProperty);
        }

        [Fact]
        public void SortSensors_WithNewProperty_SortsAscending()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.Sensors.Add(new SensorOperationalModel { Id = 2 });
            viewModel.Sensors.Add(new SensorOperationalModel { Id = 1 });

            // Act
            viewModel.SortCommand.Execute("Id");

            // Assert
            Assert.Equal("Id", viewModel.SortProperty);
            Assert.True(viewModel.IsSortAscending);
            Assert.Equal(1, viewModel.Sensors[0].Id);
            Assert.Equal(2, viewModel.Sensors[1].Id);
        }

        [Fact]
        public void SortSensors_WithSameProperty_TogglesSortDirection()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.Sensors.Add(new SensorOperationalModel { Id = 1 });
            viewModel.Sensors.Add(new SensorOperationalModel { Id = 2 });
            viewModel.SortProperty = "Id";
            viewModel.IsSortAscending = true;

            // Act - sort again on the same property
            viewModel.SortCommand.Execute("Id");

            // Assert - direction should toggle to descending
            Assert.Equal("Id", viewModel.SortProperty);
            Assert.False(viewModel.IsSortAscending);
            Assert.Equal(2, viewModel.Sensors[0].Id);
            Assert.Equal(1, viewModel.Sensors[1].Id);
        }

        [Fact]
        public void GetSortIndicator_WithMatchingProperty_ReturnsDirectionIndicator()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.SortProperty = "Id";

            // Act & Assert - for ascending
            viewModel.IsSortAscending = true;
            Assert.Equal(" ▲", viewModel.GetSortIndicator("Id"));

            // Act & Assert - for descending
            viewModel.IsSortAscending = false;
            Assert.Equal(" ▼", viewModel.GetSortIndicator("Id"));
        }

        [Fact]
        public void GetSortIndicator_WithNonMatchingProperty_ReturnsEmptyString()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.SortProperty = "Id";

            // Act & Assert
            Assert.Equal(string.Empty, viewModel.GetSortIndicator("Type"));
        }

        [Fact]
        public void SortIndicatorProperties_ReturnCorrectIndicators()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.SortProperty = "Id";
            viewModel.IsSortAscending = true;

            // Act & Assert - check all properties
            Assert.Equal(" ▲", viewModel.IdSortIndicator);
            Assert.Equal(string.Empty, viewModel.TypeSortIndicator);
            Assert.Equal(string.Empty, viewModel.StatusSortIndicator);
            Assert.Equal(string.Empty, viewModel.MeasurandSortIndicator);
            Assert.Equal(string.Empty, viewModel.DeploymentDateSortIndicator);
            Assert.Equal(string.Empty, viewModel.IncidentCountSortIndicator);

            // Change sort property
            viewModel.SortProperty = "IncidentCount";

            // Assert again
            Assert.Equal(string.Empty, viewModel.IdSortIndicator);
            Assert.Equal(" ▲", viewModel.IncidentCountSortIndicator);
        }
    }

}
