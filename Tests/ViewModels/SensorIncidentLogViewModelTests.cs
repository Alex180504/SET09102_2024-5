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
    public class SensorIncidentLogViewModelTests
    {
        private readonly List<Sensor> _testSensors;
        private readonly List<Incident> _testIncidents;
        private readonly List<Measurement> _testMeasurements;
        private readonly List<IncidentMeasurement> _testIncidentMeasurements;
        private readonly User _testUser;
        private readonly MockMainThreadService _mockMainThreadService;
        private readonly MockDialogService _mockDialogService;

        public SensorIncidentLogViewModelTests()
        {
            // Create test user (responder)
            _testUser = new User
            {
                UserId = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                PasswordHash = "hashedpassword123",
                PasswordSalt = "salt123"
            };

            // Create test sensor with measurand
            var measurand = new Measurand
            {
                MeasurandId = 1,
                QuantityName = "Temperature",
                QuantityType = "Physical",
                Symbol = "T",
                Unit = "°C"
            };

            _testSensors = new List<Sensor>
            {
                new Sensor
                {
                    SensorId = 1,
                    SensorType = "Temperature",
                    Status = "Active",
                    MeasurandId = 1,
                    Measurand = measurand
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
                    ResponderComments = "Investigating temperature spike",
                    ResponderId = 1,
                    Responder = _testUser,
                    IncidentMeasurements = new List<IncidentMeasurement>()
                },
                new Incident
                {
                    IncidentId = 2,
                    Priority = "Medium",
                    ResolvedDate = DateTime.Now.AddDays(-5),
                    ResponderComments = "Fixed sensor calibration",
                    ResponderId = 1,
                    Responder = _testUser,
                    IncidentMeasurements = new List<IncidentMeasurement>()
                },
                new Incident
                {
                    IncidentId = 3,
                    Priority = "Low",
                    ResolvedDate = null,
                    ResponderComments = "Monitoring for additional issues",
                    ResponderId = null,
                    Responder = null,
                    IncidentMeasurements = new List<IncidentMeasurement>()
                }
            };

            // Create test measurements for the sensor
            _testMeasurements = new List<Measurement>
            {
                new Measurement
                {
                    MeasurementId = 1,
                    SensorId = 1,
                    Sensor = _testSensors[0],
                    Value = 25.5f,
                    Timestamp = DateTime.Now.AddDays(-10),
                    IncidentMeasurements = new List<IncidentMeasurement>()
                },
                new Measurement
                {
                    MeasurementId = 2,
                    SensorId = 1,
                    Sensor = _testSensors[0],
                    Value = 30.0f,
                    Timestamp = DateTime.Now.AddDays(-9),
                    IncidentMeasurements = new List<IncidentMeasurement>()
                },
                new Measurement
                {
                    MeasurementId = 3,
                    SensorId = 1,
                    Sensor = _testSensors[0],
                    Value = 35.0f,
                    Timestamp = DateTime.Now.AddDays(-8),
                    IncidentMeasurements = new List<IncidentMeasurement>()
                }
            };

            // Create connections between measurements and incidents
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
                    IncidentId = 2,
                    Measurement = _testMeasurements[1],
                    Incident = _testIncidents[1]
                },
                new IncidentMeasurement
                {
                    MeasurementId = 3,
                    IncidentId = 3,
                    Measurement = _testMeasurements[2],
                    Incident = _testIncidents[2]
                }
            };

            // Link incident measurements to measurements and incidents
            _testMeasurements[0].IncidentMeasurements.Add(_testIncidentMeasurements[0]);
            _testMeasurements[1].IncidentMeasurements.Add(_testIncidentMeasurements[1]);
            _testMeasurements[2].IncidentMeasurements.Add(_testIncidentMeasurements[2]);

            _testIncidents[0].IncidentMeasurements.Add(_testIncidentMeasurements[0]);
            _testIncidents[1].IncidentMeasurements.Add(_testIncidentMeasurements[1]);
            _testIncidents[2].IncidentMeasurements.Add(_testIncidentMeasurements[2]);

            // Initialize mock services
            _mockMainThreadService = new MockMainThreadService();
            _mockDialogService = new MockDialogService();
        }

        private SensorMonitoringContext CreateDbContext(string dbName = null)
        {
            // Create a unique database name if none provided
            dbName ??= $"TestIncidentLogDb_{Guid.NewGuid()}";

            var options = new DbContextOptionsBuilder<SensorMonitoringContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            var context = new SensorMonitoringContext(options);

            // Clear database to ensure a clean state
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            return context;
        }

        private async Task<SensorIncidentLogViewModel> CreateViewModelWithDataAsync(string dbName = null)
        {
            var context = CreateDbContext(dbName);

            // Add test data
            await context.Users.AddAsync(_testUser);
            await context.Sensors.AddRangeAsync(_testSensors);
            await context.Incidents.AddRangeAsync(_testIncidents);
            await context.Measurements.AddRangeAsync(_testMeasurements);
            await context.IncidentMeasurements.AddRangeAsync(_testIncidentMeasurements);
            await context.SaveChangesAsync();

            return new SensorIncidentLogViewModel(
                context,
                _mockMainThreadService,
                _mockDialogService);
        }

        private SensorIncidentLogViewModel CreateViewModel(string dbName = null)
        {
            var context = CreateDbContext(dbName);

            return new SensorIncidentLogViewModel(
                context,
                _mockMainThreadService,
                _mockDialogService);
        }

        [Fact]
        public void Constructor_InitializesProperties()
        {
            // Arrange & Act
            var viewModel = CreateViewModel();

            // Assert
            Assert.NotNull(viewModel.Incidents);
            Assert.NotNull(viewModel.LoadIncidentsCommand);
            Assert.NotNull(viewModel.ApplyCommand);
            Assert.NotNull(viewModel.SortCommand);
            Assert.NotNull(viewModel.BackCommand);
            Assert.Equal("", viewModel.SortProperty);
            Assert.Equal("", viewModel.SortIndicator);
            Assert.True(viewModel.IsSortAscending);
            Assert.False(viewModel.IsLoading);
            Assert.NotNull(viewModel.FilterProperties);
            Assert.Equal(5, viewModel.FilterProperties.Count); // "All", "ID", "Priority", "Status", "Responder"
            Assert.Equal("All", viewModel.SelectedFilterProperty);
        }

        [Fact]
        public void Constructor_WithNullContext_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new SensorIncidentLogViewModel(
                    null,
                    _mockMainThreadService,
                    _mockDialogService));
        }

        [Fact]
        public void ApplyQueryAttributes_WithValidSensorId_SetsSensorId()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var query = new Dictionary<string, object>
            {
                { "SensorId", "5" }
            };

            // Act
            viewModel.ApplyQueryAttributes(query);

            // Assert
            Assert.Equal(5, viewModel.SensorId);
        }

        [Fact]
        public void ApplyQueryAttributes_WithInvalidSensorId_DoesNotSetSensorId()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.SensorId = 0;
            var query = new Dictionary<string, object>
            {
                { "SensorId", "invalid" }
            };

            // Act
            viewModel.ApplyQueryAttributes(query);

            // Assert
            Assert.Equal(0, viewModel.SensorId);
        }

        [Fact]
        public void ApplyQueryAttributes_WithMissingSensorId_DoesNotSetSensorId()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.SensorId = 0;
            var query = new Dictionary<string, object>
            {
                { "OtherParam", "value" }
            };

            // Act
            viewModel.ApplyQueryAttributes(query);

            // Assert
            Assert.Equal(0, viewModel.SensorId);
        }

        [Fact]
        public async Task LoadIncidentsAsync_WithValidSensor_LoadsAndMapsIncidents()
        {
            // Arrange
            var viewModel = await CreateViewModelWithDataAsync();
            viewModel.SensorId = 1; // Set valid sensor ID
            viewModel.Incidents.Clear();

            // Act
            viewModel.LoadIncidentsCommand.Execute(null);
            await Task.Delay(100); // Small delay to let async operation complete

            // Assert
            Assert.Equal(3, viewModel.Incidents.Count);
            Assert.Contains($"Sensor 1 - Temperature", viewModel.SensorInfo);

            // Verify incident data is mapped correctly
            var firstIncident = viewModel.Incidents.First(i => i.Id == 1);
            Assert.Equal("High", firstIncident.Priority);
            Assert.Equal("Open", firstIncident.Status); // Not resolved, so "Open"
            Assert.Equal("John Doe", firstIncident.ResponderName);
            Assert.Contains("temperature spike", firstIncident.ResponderComments);
            Assert.Null(firstIncident.ResolvedDate);

            var secondIncident = viewModel.Incidents.First(i => i.Id == 2);
            Assert.Equal("Medium", secondIncident.Priority);
            Assert.Equal("Resolved", secondIncident.Status);
            Assert.Equal("John Doe", secondIncident.ResponderName);

            var thirdIncident = viewModel.Incidents.First(i => i.Id == 3);
            Assert.Equal("Unassigned", thirdIncident.ResponderName); // Null responder should be "Unassigned"
        }

        [Fact]
        public async Task LoadIncidentsAsync_WithInvalidSensor_ShowsErrorMessage()
        {
            // Arrange
            var viewModel = await CreateViewModelWithDataAsync();
            viewModel.SensorId = 999; // Invalid sensor ID
            viewModel.Incidents.Clear();
            _mockDialogService.DisplayedMessages.Clear();

            // Act
            viewModel.LoadIncidentsCommand.Execute(null);
            await Task.Delay(100); // Small delay to let async operation complete

            // Assert
            Assert.Empty(viewModel.Incidents);
            Assert.Contains(_mockDialogService.DisplayedMessages, m => m.Contains("not found"));
        }

        [Fact]
        public void Incidents_WhenSet_UpdatesHasIncidentsAndHasNoIncidents()
        {
            // Arrange
            var viewModel = CreateViewModel();

            // Act - set empty collection
            viewModel.Incidents = new ObservableCollection<IncidentModel>();

            // Assert
            Assert.False(viewModel.HasIncidents);
            Assert.True(viewModel.HasNoIncidents);

            // Act - add an item
            viewModel.Incidents.Add(new IncidentModel { Id = 1 });

            // Assert
            Assert.True(viewModel.HasIncidents);
            Assert.False(viewModel.HasNoIncidents);
        }

        [Fact]
        public void IsLoading_WhenSet_UpdatesCommandCanExecute()
        {
            // Arrange
            var viewModel = CreateViewModel();
            bool initialLoadIncidentsCanExecute = viewModel.LoadIncidentsCommand.CanExecute(null);
            bool initialApplyCanExecute = viewModel.ApplyCommand.CanExecute(null);

            // Act
            viewModel.IsLoading = true;

            // Assert
            Assert.True(initialLoadIncidentsCanExecute);
            Assert.True(initialApplyCanExecute);
            Assert.False(viewModel.LoadIncidentsCommand.CanExecute(null));
            Assert.False(viewModel.ApplyCommand.CanExecute(null));
        }

        [Fact]
        public async Task ApplyFilterAndRefresh_WithEmptyFilter_ReloadsAllIncidents()
        {
            // Arrange
            var viewModel = await CreateViewModelWithDataAsync();
            viewModel.SensorId = 1;
            viewModel.LoadIncidentsCommand.Execute(null);
            await Task.Delay(100); // Allow async operations to complete

            viewModel.FilterText = "";

            // Act
            viewModel.ApplyCommand.Execute(null);
            await Task.Delay(100); // Allow async operations to complete

            // Assert
            Assert.Equal(3, viewModel.Incidents.Count);
        }

        [Fact]
        public async Task ApplyFilter_WithIdFilter_FiltersCorrectly()
        {
            // Arrange
            var viewModel = await CreateViewModelWithDataAsync();
            viewModel.SensorId = 1;
            viewModel.LoadIncidentsCommand.Execute(null);
            await Task.Delay(100); // Allow async operations to complete

            viewModel.SelectedFilterProperty = "ID";
            viewModel.FilterText = "1"; // ID of first incident

            // Use reflection to access private method
            var method = typeof(SensorIncidentLogViewModel).GetMethod(
                "ApplyFilter",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            method.Invoke(viewModel, null);

            // Assert
            Assert.Single(viewModel.Incidents);
            Assert.Equal(1, viewModel.Incidents[0].Id);
        }

        [Fact]
        public async Task ApplyFilter_WithPriorityFilter_FiltersCorrectly()
        {
            // Arrange
            var viewModel = await CreateViewModelWithDataAsync();
            viewModel.SensorId = 1;
            viewModel.LoadIncidentsCommand.Execute(null);
            await Task.Delay(100); // Allow async operations to complete

            viewModel.SelectedFilterProperty = "Priority";
            viewModel.FilterText = "high"; // Priority of first incident

            // Use reflection to access private method
            var method = typeof(SensorIncidentLogViewModel).GetMethod(
                "ApplyFilter",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            method.Invoke(viewModel, null);

            // Assert
            Assert.Single(viewModel.Incidents);
            Assert.Equal("High", viewModel.Incidents[0].Priority);
        }

        [Fact]
        public async Task ApplyFilter_WithStatusFilter_FiltersCorrectly()
        {
            // Arrange
            var viewModel = await CreateViewModelWithDataAsync();
            viewModel.SensorId = 1;
            viewModel.LoadIncidentsCommand.Execute(null);
            await Task.Delay(100); // Allow async operations to complete

            viewModel.SelectedFilterProperty = "Status";
            viewModel.FilterText = "open"; // Two incidents are "Open"

            // Use reflection to access private method
            var method = typeof(SensorIncidentLogViewModel).GetMethod(
                "ApplyFilter",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            method.Invoke(viewModel, null);

            // Assert - should find two incidents with "Open" status
            Assert.Equal(2, viewModel.Incidents.Count);
            Assert.All(viewModel.Incidents, i => Assert.Equal("Open", i.Status));
        }

        [Fact]
        public async Task ApplyFilter_WithResponderFilter_FiltersCorrectly()
        {
            // Arrange
            var viewModel = await CreateViewModelWithDataAsync();
            viewModel.SensorId = 1;
            viewModel.LoadIncidentsCommand.Execute(null);
            await Task.Delay(100); // Allow async operations to complete

            viewModel.SelectedFilterProperty = "Responder";
            viewModel.FilterText = "john"; // First name of responder

            // Use reflection to access private method
            var method = typeof(SensorIncidentLogViewModel).GetMethod(
                "ApplyFilter",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            method.Invoke(viewModel, null);

            // Assert
            Assert.Equal(2, viewModel.Incidents.Count); // 2 incidents have John Doe as responder
            Assert.All(viewModel.Incidents, i => Assert.Contains("John", i.ResponderName));
        }

        [Fact]
        public async Task ApplyFilter_WithAllFilter_FiltersAcrossAllProperties()
        {
            // Arrange
            var viewModel = await CreateViewModelWithDataAsync();
            viewModel.SensorId = 1;
            viewModel.LoadIncidentsCommand.Execute(null);
            await Task.Delay(100); // Allow async operations to complete

            viewModel.SelectedFilterProperty = "All";
            viewModel.FilterText = "monitoring"; // Exists in ResponderComments

            // Use reflection to access private method
            var method = typeof(SensorIncidentLogViewModel).GetMethod(
                "ApplyFilter",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            method.Invoke(viewModel, null);

            // Assert
            Assert.Single(viewModel.Incidents);
            Assert.Contains("Monitoring", viewModel.Incidents[0].ResponderComments);
        }

        [Fact]
        public void SortIncidents_WithNewProperty_SortsAscending()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.Incidents = new ObservableCollection<IncidentModel>
            {
                new IncidentModel { Id = 2, Priority = "Medium" },
                new IncidentModel { Id = 1, Priority = "High" }
            };

            // Act
            viewModel.SortCommand.Execute("Id");

            // Assert
            Assert.Equal("Id", viewModel.SortProperty);
            Assert.True(viewModel.IsSortAscending);
            Assert.Equal(1, viewModel.Incidents[0].Id);
            Assert.Equal(2, viewModel.Incidents[1].Id);
        }

        [Fact]
        public void SortIncidents_WithSameProperty_TogglesSortDirection()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.Incidents = new ObservableCollection<IncidentModel>
            {
                new IncidentModel { Id = 1, Priority = "High" },
                new IncidentModel { Id = 2, Priority = "Medium" }
            };

            viewModel.SortProperty = "Id";
            viewModel.IsSortAscending = true;

            // Act - sort again on the same property
            viewModel.SortCommand.Execute("Id");

            // Assert - direction should toggle to descending
            Assert.Equal("Id", viewModel.SortProperty);
            Assert.False(viewModel.IsSortAscending);
            Assert.Equal(2, viewModel.Incidents[0].Id);
            Assert.Equal(1, viewModel.Incidents[1].Id);
        }

        [Fact]
        public void ApplySorting_UpdatesSortIndicatorsWhenRequested()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.Incidents = new ObservableCollection<IncidentModel>
            {
                new IncidentModel { Id = 1, Priority = "High" },
                new IncidentModel { Id = 2, Priority = "Medium" }
            };

            // Use reflection to access private method
            var method = typeof(SensorIncidentLogViewModel).GetMethod(
                "ApplySorting",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            viewModel.SortProperty = "Priority";
            viewModel.IsSortAscending = true;
            method.Invoke(viewModel, new object[] { "Priority", true });

            // Assert
            Assert.Equal("▲", viewModel.SortIndicator);
            Assert.Equal(" ▲", viewModel.PrioritySortIndicator);
            Assert.Equal(string.Empty, viewModel.IdSortIndicator);

            // Change direction
            viewModel.IsSortAscending = false;
            method.Invoke(viewModel, new object[] { "Priority", true });

            // Assert
            Assert.Equal("▼", viewModel.SortIndicator);
            Assert.Equal(" ▼", viewModel.PrioritySortIndicator);
        }

        [Fact]
        public void GetSortIndicator_ReturnsCorrectIndicators()
        {
            // Arrange
            var viewModel = CreateViewModel();

            // Test ascending indicator
            viewModel.SortProperty = "Id";
            viewModel.IsSortAscending = true;

            // Assert - matching property gets indicator
            Assert.Equal(" ▲", viewModel.GetSortIndicator("Id"));
            Assert.Equal(string.Empty, viewModel.GetSortIndicator("Priority"));

            // Test descending indicator
            viewModel.IsSortAscending = false;

            // Assert
            Assert.Equal(" ▼", viewModel.GetSortIndicator("Id"));
        }

        [Fact]
        public void SortIndicatorProperties_ReturnCorrectValues()
        {
            // Arrange
            var viewModel = CreateViewModel();

            // Case 1: Id is sorted ascending
            viewModel.SortProperty = "Id";
            viewModel.IsSortAscending = true;

            // Assert all indicator properties
            Assert.Equal(" ▲", viewModel.IdSortIndicator);
            Assert.Equal(string.Empty, viewModel.PrioritySortIndicator);
            Assert.Equal(string.Empty, viewModel.StatusSortIndicator);
            Assert.Equal(string.Empty, viewModel.ResponderSortIndicator);
            Assert.Equal(string.Empty, viewModel.ResolvedDateSortIndicator);

            // Case 2: Status is sorted descending
            viewModel.SortProperty = "Status";
            viewModel.IsSortAscending = false;

            // Assert again
            Assert.Equal(string.Empty, viewModel.IdSortIndicator);
            Assert.Equal(" ▼", viewModel.StatusSortIndicator);
            Assert.Equal(string.Empty, viewModel.PrioritySortIndicator);
        }
    }
}
