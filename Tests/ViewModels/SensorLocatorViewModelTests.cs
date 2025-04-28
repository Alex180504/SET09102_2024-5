using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Storage;
using Moq;
using SET09102_2024_5.Data.Repositories;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using SET09102_2024_5.ViewModels;
using Xunit;
using Tests.Mocks;

namespace SET09102_2024_5.Tests.ViewModels
{
    public class SensorLocatorViewModelTests : IDisposable
    {
        private readonly Mock<ISensorService> _mockSensorService;
        private readonly Mock<IMainThreadService> _mockMainThreadService;
        private readonly Mock<IMeasurementRepository> _mockMeasurementRepo;
        private readonly Mock<IDialogService> _mockDialogService;
        private readonly Mock<ILogger<SensorLocatorViewModel>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly List<Sensor> _testSensors;
        private readonly SensorLocatorViewModel _viewModel;
        private readonly Mock<IDispatcher> _mockDispatcher;
        private readonly Mock<IDispatcherTimer> _mockTimer;

        // Store original Application.Current to restore it later
        private readonly Application _originalApplication;

        public SensorLocatorViewModelTests()
        {
            // Save original Application.Current
            _originalApplication = Application.Current;

            // Initialize mocks
            _mockSensorService = new Mock<ISensorService>();
            _mockMainThreadService = new Mock<IMainThreadService>();
            _mockMeasurementRepo = new Mock<IMeasurementRepository>();
            _mockDialogService = new Mock<IDialogService>();
            _mockLogger = new Mock<ILogger<SensorLocatorViewModel>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockDispatcher = new Mock<IDispatcher>();
            _mockTimer = new Mock<IDispatcherTimer>();

            // Setup mock dispatcher timer
            _mockTimer.Setup(t => t.Start());
            _mockTimer.Setup(t => t.Stop());
            _mockTimer.SetupAllProperties();
            _mockDispatcher.Setup(d => d.CreateTimer()).Returns(_mockTimer.Object);

            // Create a mock application with dispatcher
            Application.Current = new MockApplication(_mockDispatcher.Object);

            // Setup configuration for API key
            _mockConfiguration.Setup(c => c["OpenRouteServiceApiKey"])
                .Returns("test-api-key");

            // Setup MainThreadService to execute actions immediately
            _mockMainThreadService.Setup(m => m.BeginInvokeOnMainThread(It.IsAny<Action>()))
                .Callback<Action>(action => action());

            // Setup Mapsui.Styles.BitmapRegistry.Instance
            SetupMockBitmapRegistry();

            // Create test sensors for testing
            _testSensors = new List<Sensor>
            {
                new Sensor
                {
                    SensorId = 1,
                    SensorType = "Temperature",
                    Status = "Active",
                    DisplayName = "Temperature Sensor 1",
                    Measurand = new Measurand
                    {
                        MeasurandId = 1,
                        QuantityName = "Temperature",
                        QuantityType = "Physical",
                        Symbol = "T",
                        Unit = "°C"
                    },
                    Configuration = new Configuration
                    {
                        SensorId = 1,
                        Latitude = 55.953251f,
                        Longitude = -3.188267f,
                        MeasurementFrequency = 5
                    }
                },
                new Sensor
                {
                    SensorId = 2,
                    SensorType = "Humidity",
                    Status = "Inactive",
                    DisplayName = "Humidity Sensor 1",
                    Measurand = new Measurand
                    {
                        MeasurandId = 2,
                        QuantityName = "Humidity",
                        QuantityType = "Physical",
                        Symbol = "RH",
                        Unit = "%"
                    },
                    Configuration = new Configuration
                    {
                        SensorId = 2,
                        Latitude = 55.952452f,
                        Longitude = -3.191017f,
                        MeasurementFrequency = 10
                    }
                },
                new Sensor
                {
                    SensorId = 3,
                    SensorType = "Air Quality",
                    Status = "Warning",
                    DisplayName = null, // Test with null display name
                    Measurand = new Measurand
                    {
                        MeasurandId = 3,
                        QuantityName = "Air Quality",
                        QuantityType = "Environmental",
                        Symbol = "AQI",
                        Unit = "ppm"
                    },
                    Configuration = null // Test with null configuration
                }
            };

            // Setup sensor service to return test sensors
            _mockSensorService.Setup(s => s.GetAllWithConfigurationAsync())
                .ReturnsAsync(_testSensors);

            // Mock Geolocation and FileSystem using different approach
            SetupStaticMethodMocks();

            try
            {
                // Create the view model for testing
                _viewModel = new SensorLocatorViewModel(
                    _mockSensorService.Object,
                    _mockMainThreadService.Object,
                    _mockMeasurementRepo.Object,
                    _mockDialogService.Object,
                    _mockLogger.Object,
                    _mockConfiguration.Object);

                // Inject test sensors directly into the view model using reflection
                var sensorsField = typeof(SensorLocatorViewModel)
                    .GetField("_sensors", BindingFlags.NonPublic | BindingFlags.Instance);
                sensorsField?.SetValue(_viewModel, _testSensors);

                // Replace refresh timer
                var refreshTimerField = typeof(SensorLocatorViewModel)
                    .GetField("_refreshTimer", BindingFlags.NonPublic | BindingFlags.Instance);
                refreshTimerField?.SetValue(_viewModel, _mockTimer.Object);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating SensorLocatorViewModel: {ex}");
                throw;
            }
        }

        private void SetupMockBitmapRegistry()
        {
            try
            {
                // Try to find the BitmapRegistry type
                Type bitmapRegistryType = typeof(BitmapRegistry);

                // Create a mock registry instance
                var mockRegistry = new MockMapsuiBitmapRegistry();

                // Use reflection to set the static Instance field
                var instanceField = bitmapRegistryType.GetField("_instance",
                    BindingFlags.NonPublic | BindingFlags.Static);
                instanceField?.SetValue(null, mockRegistry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up BitmapRegistry mock: {ex}");
            }
        }

        private void SetupStaticMethodMocks()
        {
            try
            {
                // Mock the Geolocation.GetLocationAsync method
                var mockLocation = new Location(55.953251, -3.188267);
                var mockLocationTask = Task.FromResult(mockLocation);

                // Get the static method
                var getLocationAsyncMethod = typeof(Geolocation).GetMethod("GetLocationAsync",
                    BindingFlags.Public | BindingFlags.Static);

                if (getLocationAsyncMethod != null)
                {
                    // Create a dynamic method that returns our mock location
                    var mockMethod = new MockStaticMethod(
                        (GeolocationRequest request) => mockLocationTask);

                    // Store the original method for restoration
                    var originalDelegate = Delegate.CreateDelegate(
                        typeof(Func<GeolocationRequest, Task<Location>>),
                        getLocationAsyncMethod);

                    // Use reflection to replace the original method delegate
                    var delegateField = typeof(Geolocation).GetField("s_getLocationAsync",
                        BindingFlags.NonPublic | BindingFlags.Static);
                    delegateField?.SetValue(null, mockMethod.GetMockDelegate());
                }

                // Mock the FileSystem.OpenAppPackageFileAsync method
                var mockStream = new MemoryStream();
                var mockStreamTask = Task.FromResult<Stream>(mockStream);

                // Get the static method
                var openAppPackageFileAsyncMethod = typeof(FileSystem).GetMethod("OpenAppPackageFileAsync",
                    BindingFlags.Public | BindingFlags.Static);

                if (openAppPackageFileAsyncMethod != null)
                {
                    // Create a dynamic method
                    var mockMethod = new MockStaticMethod(
                        (string filename) => mockStreamTask);

                    // Store the original method
                    var originalDelegate = Delegate.CreateDelegate(
                        typeof(Func<string, Task<Stream>>),
                        openAppPackageFileAsyncMethod);

                    // Replace the method
                    var delegateField = typeof(FileSystem).GetField("s_openAppPackageFileAsync",
                        BindingFlags.NonPublic | BindingFlags.Static);
                    delegateField?.SetValue(null, mockMethod.GetMockDelegate());
                }

                var fileExistsMethod = typeof(FileSystem).GetMethod("AppPackageFileExistsAsync",
                    BindingFlags.Public | BindingFlags.Static);

                if (fileExistsMethod != null)
                {
                    var mockMethod = new MockStaticMethod(
                        (string filename) => Task.FromResult(true));

                    var delegateField = typeof(FileSystem).GetField("s_appPackageFileExistsAsync",
                        BindingFlags.NonPublic | BindingFlags.Static);
                    delegateField?.SetValue(null, mockMethod.GetMockDelegate());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up static method mocks: {ex}");
            }
        }

        public void Dispose()
        {
            try
            {
                _viewModel?.Dispose();

                // Restore original Application.Current
                Application.Current = _originalApplication;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during Dispose: {ex}");
            }
        }

        [Fact]
        public void FilterSensors_WithEmptyString_ShowsAllSensors()
        {
            // Act
            _viewModel.FilterSensors("");

            // Assert
            Assert.Equal(_testSensors.Count, _viewModel.FilteredSensors.Count);
            Assert.True(_viewModel.IsSearchActive);
            Assert.Contains(_viewModel.FilteredSensors, s => s.SensorId == 1);
            Assert.Contains(_viewModel.FilteredSensors, s => s.SensorId == 2);
            Assert.Contains(_viewModel.FilteredSensors, s => s.SensorId == 3);
        }

        [Fact]
        public void FilterSensors_WithNull_ShowsAllSensors()
        {
            // Act
            _viewModel.FilterSensors(null);

            // Assert
            Assert.Equal(_testSensors.Count, _viewModel.FilteredSensors.Count);
            Assert.True(_viewModel.IsSearchActive);
        }

        [Fact]
        public void FilterSensors_MatchingSensorType_ReturnsMatchingSensors()
        {
            // Act
            _viewModel.FilterSensors("temperature");

            // Assert
            Assert.Single(_viewModel.FilteredSensors);
            Assert.Equal(1, _viewModel.FilteredSensors[0].SensorId);
        }

        [Fact]
        public void FilterSensors_MatchingMeasurandName_ReturnsMatchingSensors()
        {
            // Act
            _viewModel.FilterSensors("humidity");

            // Assert
            Assert.Single(_viewModel.FilteredSensors);
            Assert.Equal(2, _viewModel.FilteredSensors[0].SensorId);
        }

        [Fact]
        public void FilterSensors_MatchingDisplayNameSubstring_ReturnsMatchingSensors()
        {
            // Act
            _viewModel.FilterSensors("SENSOR 1"); // Case-insensitive search

            // Assert
            Assert.Equal(2, _viewModel.FilteredSensors.Count); // Both named sensors
            Assert.Contains(_viewModel.FilteredSensors, s => s.SensorId == 1);
            Assert.Contains(_viewModel.FilteredSensors, s => s.SensorId == 2);
        }

        [Fact]
        public void FilterSensors_NotMatchingAnyProperty_ReturnsEmptyCollection()
        {
            // Act
            _viewModel.FilterSensors("xyz123notfound");

            // Assert
            Assert.Empty(_viewModel.FilteredSensors);
            Assert.True(_viewModel.IsSearchActive);
        }

        [Fact]
        public void FilterSensors_WithNullProperties_HandlesGracefully()
        {
            // Arrange - Add a sensor with all null values
            var nullSensor = new Sensor
            {
                SensorId = 4,
                SensorType = null,
                Status = null,
                DisplayName = null,
                Measurand = null,
                Configuration = null
            };
            var sensorsWithNull = new List<Sensor>(_testSensors) { nullSensor };
            var sensorsField = typeof(SensorLocatorViewModel)
                .GetField("_sensors", BindingFlags.NonPublic | BindingFlags.Instance);
            sensorsField?.SetValue(_viewModel, sensorsWithNull);

            // Act - Should not throw exceptions
            _viewModel.FilterSensors("test");

            // Assert
            Assert.True(_viewModel.IsSearchActive);
        }

        [Fact]
        public void HideSearchResults_SetsIsSearchActiveToFalse()
        {
            // Arrange
            _viewModel.FilterSensors(""); // Sets IsSearchActive to true
            Assert.True(_viewModel.IsSearchActive);

            // Act
            _viewModel.HideSearchResults();

            // Assert
            Assert.False(_viewModel.IsSearchActive);
        }

        [Fact]
        public void HideSearchResults_WhenAlreadyHidden_StillSetsFalse()
        {
            // Arrange
            _viewModel.IsSearchActive = false;

            // Act
            _viewModel.HideSearchResults();

            // Assert
            Assert.False(_viewModel.IsSearchActive);
        }

        [Fact]
        public async Task HandlePinTappedAsync_WithValidSensorId_DisplaysDialog()
        {
            // Arrange
            var mapInfo = new MapInfo();
            // Create a PointFeature with x,y coordinates
            var merc = SphericalMercator.FromLonLat(-3.188267, 55.953251);
            var feature = new PointFeature(merc.x, merc.y);
            mapInfo.Feature = feature;
            feature["SensorId"] = 1; // Valid sensor ID

            // Setup dialog service to return true (user clicked "Navigate to Sensor")
            _mockDialogService.Setup(d => d.DisplayConfirmationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ReturnsAsync(true);

            // Act
            await _viewModel.HandlePinTappedAsync(mapInfo);

            // Assert
            _mockDialogService.Verify(d => d.DisplayConfirmationAsync(
                It.Is<string>(s => s.Contains("Temperature Sensor 1")),
                It.IsAny<string>(),
                "Navigate to Sensor",
                "Close"
            ), Times.Once);
        }

        [Fact]
        public async Task HandlePinTappedAsync_WithNonExistentSensorId_DoesNothing()
        {
            // Arrange
            var mapInfo = new MapInfo();
            var merc = SphericalMercator.FromLonLat(-3.188267, 55.953251);
            var feature = new PointFeature(merc.x, merc.y);
            mapInfo.Feature = feature;
            feature["SensorId"] = 999; // Non-existent sensor ID

            // Act
            await _viewModel.HandlePinTappedAsync(mapInfo);

            // Assert
            _mockDialogService.Verify(d => d.DisplayConfirmationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ), Times.Never);
        }

        [Fact]
        public async Task HandlePinTappedAsync_WithNullFeature_DoesNothing()
        {
            // Arrange
            var mapInfo = new MapInfo
            {
                Feature = null
            };

            // Act
            await _viewModel.HandlePinTappedAsync(mapInfo);

            // Assert
            _mockDialogService.Verify(d => d.DisplayConfirmationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ), Times.Never);
        }

        [Fact]
        public async Task HandlePinTappedAsync_WithoutSensorIdProperty_DoesNothing()
        {
            // Arrange
            var mapInfo = new MapInfo();
            var merc = SphericalMercator.FromLonLat(-3.188267, 55.953251);
            var feature = new PointFeature(merc.x, merc.y);
            mapInfo.Feature = feature;

            // Act
            await _viewModel.HandlePinTappedAsync(mapInfo);

            // Assert
            _mockDialogService.Verify(d => d.DisplayConfirmationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ), Times.Never);
        }

        [Fact]
        public async Task HandlePinTappedAsync_WithNonIntegerSensorId_DoesNothing()
        {
            // Arrange
            var mapInfo = new MapInfo();
            var merc = SphericalMercator.FromLonLat(-3.188267, 55.953251);
            var feature = new PointFeature(merc.x, merc.y);
            mapInfo.Feature = feature;
            feature["SensorId"] = "NotAnInteger"; // Wrong type

            // Act
            await _viewModel.HandlePinTappedAsync(mapInfo);

            // Assert
            _mockDialogService.Verify(d => d.DisplayConfirmationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ), Times.Never);
        }

        [Fact]
        public async Task HandlePinTappedAsync_WithSensorHavingNullDisplayName_UsesFallbackTitle()
        {
            // Arrange
            var mapInfo = new MapInfo();
            var merc = SphericalMercator.FromLonLat(-3.19f, 55.95f); // Some default coordinates
            var feature = new PointFeature(merc.x, merc.y);
            mapInfo.Feature = feature;
            feature["SensorId"] = 3; // Sensor with null display name

            // Setup dialog service
            _mockDialogService.Setup(d => d.DisplayConfirmationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ReturnsAsync(false); // User clicks "Close"

            // Act
            await _viewModel.HandlePinTappedAsync(mapInfo);

            // Assert
            _mockDialogService.Verify(d => d.DisplayConfirmationAsync(
                It.Is<string>(s => s.Contains("Sensor 3")), // Should use fallback title
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ), Times.Once);
        }

        [Fact]
        public async Task HandlePinTappedAsync_WithSensorHavingNoCoordinates_ShowsNACoordinates()
        {
            // Arrange
            var mapInfo = new MapInfo();
            var merc = SphericalMercator.FromLonLat(-3.19f, 55.95f); // Some default coordinates
            var feature = new PointFeature(merc.x, merc.y);
            mapInfo.Feature = feature;
            feature["SensorId"] = 3; // Sensor with null configuration

            // Setup dialog service
            _mockDialogService.Setup(d => d.DisplayConfirmationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ReturnsAsync(false);

            // Act
            await _viewModel.HandlePinTappedAsync(mapInfo);

            // Assert
            _mockDialogService.Verify(d => d.DisplayConfirmationAsync(
                It.IsAny<string>(),
                It.Is<string>(msg => msg.Contains("Coordinates: N/A")),
                It.IsAny<string>(),
                It.IsAny<string>()
            ), Times.Once);
        }

        [Fact]
        public async Task HandlePinTappedAsync_UserChoosesToNavigate_CallsNavigateToSensor()
        {
            // Arrange
            var mapInfo = new MapInfo();
            var merc = SphericalMercator.FromLonLat(-3.188267, 55.953251);
            var feature = new PointFeature(merc.x, merc.y);
            mapInfo.Feature = feature;
            feature["SensorId"] = 1;

            // Setup dialog service to return true (user clicked "Navigate to Sensor")
            _mockDialogService.Setup(d => d.DisplayConfirmationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )).ReturnsAsync(true);

            // Act
            await _viewModel.HandlePinTappedAsync(mapInfo);

            // Assert - verify route waypoints were updated
            Assert.Contains(_viewModel.RouteWaypoints, s => s.SensorId == 1);
        }

        [Fact]
        public void Dispose_CleansUpResources()
        {
            // Act
            _viewModel.Dispose();

            // Assert
            _mockTimer.Verify(t => t.Stop(), Times.Once());

            // Verify the method can be called multiple times without error
            _viewModel.Dispose();
        }
    }    
}
