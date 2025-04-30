using System.Collections.ObjectModel;
using System.Windows.Input;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Microsoft.Extensions.Logging;
using SET09102_2024_5.Data.Repositories;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Map = Mapsui.Map;
using Microsoft.Extensions.Configuration;
using Mapsui.Nts;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using SkiaSharp;


namespace SET09102_2024_5.ViewModels
{
    /// <summary>
    /// Represents the available modes of transportation for route navigation.
    /// </summary>
    public enum TravelMode
    {
        Walking,
        Driving
    }

    /// <summary>
    /// ViewModel for the sensor locator feature that provides map-based visualization,
    /// search functionality, and navigation to sensors. Integrates with OpenRouteService
    /// for route planning and displays sensor locations using interactive map pins.
    /// </summary>
    public class SensorLocatorViewModel : BaseViewModel, IDisposable
    {
        private readonly ISensorService _sensorService;
        private readonly IMainThreadService _mainThread;
        private readonly IMeasurementRepository _measurementRepo;
        private readonly IDialogService _dialogService;
        private readonly ILogger<SensorLocatorViewModel> _logger;
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private readonly List<Stream> _pinStreams = new();
        private readonly Dictionary<string, SymbolStyle> _statusStyles = new();
        private readonly string _openRouteServiceApiKey;

        private List<Sensor> _sensors = new();
        private ObservableCollection<Sensor> _filteredSensors = new();
        private Sensor _selectedSensor;
        private string _searchText;
        private bool _isSearching;
        private bool _isRouteBuilding;
        private bool _hasError;
        private string _errorMessage;
        private bool _isLoading;
        private List<Sensor> _routeWaypoints = new();
        private double _routeDistance;
        private TimeSpan _routeDuration;
        private string _routeDetailsText;
        private IDispatcherTimer _refreshTimer;
        private readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(30);

        private MemoryLayer _routeLayer;
        private MemoryLayer _pinLayer;
        private MemoryLayer _locationLayer; // Pins for the user's current location
        private Position _currentPosition;
        private int _selectedTravelModeIndex;

        /// <summary>
        /// Gets the Mapsui Map instance used for displaying the interactive map.
        /// Contains layers for sensors, routes, and the user's current location.
        /// </summary>
        public Map Map { get; }

        /// <summary>
        /// Command to execute search for sensors based on the current search text.
        /// </summary>
        public ICommand SearchCommand { get; }

        /// <summary>
        /// Command to clear the current search and hide search results.
        /// </summary>
        public ICommand ClearSearchCommand { get; }

        /// <summary>
        /// Command to refresh sensor data and update the map.
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// Command to add a sensor to the current navigation route.
        /// </summary>
        public ICommand AddToRouteCommand { get; }

        /// <summary>
        /// Command to remove a sensor from the current navigation route.
        /// </summary>
        public ICommand RemoveFromRouteCommand { get; }

        /// <summary>
        /// Command to clear all waypoints from the current route.
        /// </summary>
        public ICommand ClearRouteCommand { get; }

        /// <summary>
        /// Command to calculate and display a route between waypoints using OpenRouteService.
        /// </summary>
        public ICommand BuildRouteCommand { get; }

        /// <summary>
        /// Command to start navigation to a specific sensor from the current location.
        /// </summary>
        public ICommand NavigateToSensorCommand { get; }

        /// <summary>
        /// Command to change the travel mode used for route calculation.
        /// </summary>
        public ICommand ChangeTravelModeCommand { get; }

        public SensorLocatorViewModel(
            ISensorService sensorService,
            IMainThreadService mainThread,
            IMeasurementRepository measurementRepo,
            IDialogService dialogService,
            ILogger<SensorLocatorViewModel> logger,
            IConfiguration configuration)
        {
            _sensorService = sensorService;
            _mainThread = mainThread;
            _measurementRepo = measurementRepo;
            _dialogService = dialogService;
            _logger = logger;
            _httpClient = new HttpClient();

            // Get OpenRouteService API key from configuration
            _openRouteServiceApiKey = configuration["OpenRouteServiceApiKey"];

            // Handle missing API key gracefully
            bool hasApiKey = !string.IsNullOrEmpty(_openRouteServiceApiKey);

            if (hasApiKey)
            {
                // Set base address for OpenRouteService API
                _httpClient.BaseAddress = new Uri("https://api.openrouteservice.org/");
                _httpClient.DefaultRequestHeaders.Add("Authorization", _openRouteServiceApiKey);
            }
            else
            {
                // Log warning about missing API key
                _logger.LogWarning("OpenRouteService API key not configured. Navigation features will be limited.");
            }

            // Initialize the map and add OSM base layer
            Map = new Map();
            Map.Layers.Add(OpenStreetMap.CreateTileLayer());

            // Prepare layers
            _pinLayer = new MemoryLayer("Pins")
            {
                Features = Enumerable.Empty<IFeature>(),
                IsMapInfoLayer = true,
                Style = null // suppresses default white circle
            };
            Map.Layers.Add(_pinLayer);

            _routeLayer = new MemoryLayer("Route")
            {
                Features = Enumerable.Empty<IFeature>(),
                Style = new VectorStyle
                {
                    Fill = null,
                    Outline = null,
                    Line = { Color = Mapsui.Styles.Color.FromArgb(255, 0, 120, 240), Width = 4 }
                }
            };
            Map.Layers.Add(_routeLayer);

            _locationLayer = new MemoryLayer("CurrentLocation")
            {
                Features = Enumerable.Empty<IFeature>(),
                Style = null
            };
            Map.Layers.Add(_locationLayer);

            // Hook up the map-tap event
            Map.Info += OnMapInfo;

            // Initialize commands
            SearchCommand = new Command(ExecuteSearch);
            ClearSearchCommand = new Command(ClearSearch);
            RefreshCommand = new Command(async () => await SafeRefreshAsync());
            AddToRouteCommand = new Command<Sensor>(AddSensorToRoute, CanAddToRoute);
            RemoveFromRouteCommand = new Command<Sensor>(RemoveSensorFromRoute, CanRemoveFromRoute);
            ClearRouteCommand = new Command(ClearRoute, () => RouteWaypoints.Count > 0);
            BuildRouteCommand = new Command(async () => await BuildRouteAsync(), CanBuildRoute);
            NavigateToSensorCommand = new Command<Sensor>(NavigateToSensor);
            ChangeTravelModeCommand = new Command<TravelMode>(mode => SelectedTravelMode = mode);

            // Initialize collections
            FilteredSensors = new ObservableCollection<Sensor>();
            RouteWaypoints = new List<Sensor>();

            _refreshTimer = Application.Current.Dispatcher.CreateTimer();
            _refreshTimer.Interval = _refreshInterval;
            _refreshTimer.Tick += async (s, e) => await SafeRefreshLocationAndDataAsync();

            _selectedTravelMode = TravelMode.Walking;
            _selectedTravelModeIndex = 0;
        }

        /// <summary>
        /// Gets or sets a value indicating whether data is currently being loaded.
        /// Controls loading indicators and disables certain operations during loading.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether an error has occurred during data operations.
        /// Used to display error messages to the user.
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            private set => SetProperty(ref _hasError, value);
        }

        /// <summary>
        /// Gets or sets the error message to display when an operation fails.
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Gets or sets the text entered by the user for searching sensors.
        /// Used to filter the sensor list based on sensor name, type, or properties.
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        /// <summary>
        /// Gets or sets the collection of sensors that match the current search criteria.
        /// This collection is displayed in the search results list.
        /// </summary>
        public ObservableCollection<Sensor> FilteredSensors
        {
            get => _filteredSensors;
            set => SetProperty(ref _filteredSensors, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether search results are currently being displayed.
        /// Controls the visibility of the search results panel in the UI.
        /// </summary>
        public bool IsSearchActive
        {
            get => _isSearching;
            set => SetProperty(ref _isSearching, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the user is currently building a route.
        /// Affects the behavior of sensor selection, adding selected sensors to the route instead of centering the map.
        /// </summary>
        public bool IsRouteBuilding
        {
            get => _isRouteBuilding;
            set => SetProperty(ref _isRouteBuilding, value);
        }

        /// <summary>
        /// Gets or sets the total distance of the current route in kilometers.
        /// Used for displaying route information to the user.
        /// </summary>
        public double RouteDistance
        {
            get => _routeDistance;
            set => SetProperty(ref _routeDistance, value);
        }

        /// <summary>
        /// Gets or sets the estimated duration of the current route.
        /// Used for displaying route information to the user.
        /// Setting this value also updates the RouteDetailsText.
        /// </summary>
        public TimeSpan RouteDuration
        {
            get => _routeDuration;
            set
            {
                if (SetProperty(ref _routeDuration, value))
                {
                    UpdateRouteDetailsText();
                }
            }
        }

        /// <summary>
        /// Gets or sets the formatted text that displays route details (distance and duration).
        /// This is displayed in the UI to provide route information to the user.
        /// </summary>
        public string RouteDetailsText
        {
            get => _routeDetailsText;
            private set => SetProperty(ref _routeDetailsText, value);
        }

        /// <summary>
        /// Updates the route details text based on the current route distance and duration.
        /// Creates a formatted string combining distance and time information.
        /// </summary>
        private void UpdateRouteDetailsText()
        {
            if (RouteWaypoints.Count < 2)
            {
                RouteDetailsText = string.Empty;
                return;
            }

            var distanceText = RouteDistance > 0
                ? $"Distance: {RouteDistance:F2} km"
                : string.Empty;

            var durationText = RouteDuration > TimeSpan.Zero
                ? $"Time: {FormatTimeSpan(RouteDuration)}"
                : string.Empty;

            RouteDetailsText = string.Join(" • ", new[] { distanceText, durationText }.Where(s => !string.IsNullOrEmpty(s)));
        }
        private TravelMode _selectedTravelMode = TravelMode.Walking;
        private Dictionary<TravelMode, string> _travelModeProfiles = new()
        {
            { TravelMode.Walking, "foot-walking" },
            { TravelMode.Driving, "driving-car" }
        };

        /// <summary>
        /// Gets or sets the selected travel mode for route calculations.
        /// Changing this value triggers a route recalculation if a route is active.
        /// </summary>
        public TravelMode SelectedTravelMode
        {
            get => _selectedTravelMode;
            set
            {
                if (SetProperty(ref _selectedTravelMode, value))
                {
                    // If we have an active route, rebuild it with the new travel mode
                    if (RouteWaypoints.Count >= 2)
                    {
                        _ = BuildRouteAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the selected travel mode in the UI picker.
        /// This property bridges between the enum values and the UI control's index-based selection.
        /// </summary>
        public int SelectedTravelModeIndex
        {
            get => _selectedTravelModeIndex;
            set
            {
                if (SetProperty(ref _selectedTravelModeIndex, value))
                {
                    // Convert index to travel mode
                    SelectedTravelMode = value == 0 ? TravelMode.Walking : TravelMode.Driving;
                }
            }
        }

        /// <summary>
        /// Gets the list of available travel modes for route navigation.
        /// Used to populate the travel mode picker in the UI.
        /// </summary>
        public List<TravelMode> AvailableTravelModes => Enum.GetValues<TravelMode>().ToList();

        /// <summary>
        /// Gets or sets the currently selected sensor in the UI.
        /// Selecting a sensor either adds it to the route (if in route building mode) or
        /// centers the map on the sensor (in normal mode).
        /// </summary>
        public Sensor SelectedSensor
        {
            get => _selectedSensor;
            set
            {
                if (SetProperty(ref _selectedSensor, value) && value != null)
                {
                    if (IsRouteBuilding)
                    {
                        // If in route building mode, add to waypoints
                        AddSensorToRoute(value);
                    }
                    else
                    {
                        // Otherwise, center map on the selected sensor
                        CenterMapOnSensor(value);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the list of waypoints in the current navigation route.
        /// Each waypoint is represented by a sensor object.
        /// Changing this collection updates related properties and commands.
        /// </summary>
        public List<Sensor> RouteWaypoints
        {
            get => _routeWaypoints;
            set
            {
                if (SetProperty(ref _routeWaypoints, value))
                {
                    OnPropertyChanged(nameof(RouteWaypointsText));
                    OnPropertyChanged(nameof(NavigationTitle));
                    (RemoveFromRouteCommand as Command)?.ChangeCanExecute();
                    (ClearRouteCommand as Command)?.ChangeCanExecute();
                    (BuildRouteCommand as Command)?.ChangeCanExecute();
                }
            }
        }

        /// <summary>
        /// Gets a descriptive text about the current navigation route.
        /// Changes format based on the number of waypoints and their types.
        /// </summary>
        public string RouteWaypointsText
        {
            get
            {
                if (RouteWaypoints.Count == 0)
                    return "No navigation active";

                if (RouteWaypoints.Count == 1)
                    return $"Navigation to: {RouteWaypoints[0].DisplayName}";

                // If we have current location + sensor
                if (RouteWaypoints.Count == 2 && RouteWaypoints[0].SensorId == -1)
                    return $"Route to: {RouteWaypoints[1].DisplayName}";

                return $"Route waypoints: {RouteWaypoints.Count}";
            }
        }

        /// <summary>
        /// Gets a formatted title for the navigation route, including a travel mode icon.
        /// Used in the UI header to indicate active navigation.
        /// </summary>
        public string NavigationTitle
        {
            get
            {
                if (RouteWaypoints.Count == 0)
                    return string.Empty;

                string modeIcon = SelectedTravelMode == TravelMode.Walking ? "🚶" : "🚗";

                if (RouteWaypoints.Count == 1)
                    return $"{modeIcon} Navigating to: {RouteWaypoints[0].DisplayName}";

                // If we have current location + sensor
                if (RouteWaypoints.Count == 2 && RouteWaypoints[0].SensorId == -1)
                    return $"{modeIcon} Navigating to: {RouteWaypoints[1].DisplayName}";

                return $"{modeIcon} Navigation with {RouteWaypoints.Count} waypoints";
            }
        }

        /// <summary>
        /// Initializes the view model by loading sensor data, getting the user's location,
        /// and setting up pin styles and the refresh timer.
        /// Should be called when the view is loaded.
        /// </summary>
        public async Task InitializeAsync()
        {
            // Register pin images
            var sensorId = await RegisterPinAsync("pin_default.png");  // Green pin for default
            var selectedSensorId = await RegisterPinAsync("pin_warning.png"); // Yellow pin for the selected sensor
            var locationId = await RegisterPinAsync("my_location.png"); // User location pin

            // Only keep green (default) and yellow (selected) styles
            _statusStyles["Default"] = new SymbolStyle { BitmapId = sensorId, SymbolScale = 0.07 };
            _statusStyles["Selected"] = new SymbolStyle { BitmapId = selectedSensorId, SymbolScale = 0.10 };
            _statusStyles["Location"] = new SymbolStyle { BitmapId = locationId, SymbolScale = 0.15 };

            // Load sensors
            await SafeRefreshAsync();
            // Get current location
            await GetCurrentLocationAsync();
            // Start the refresh timer
            _refreshTimer.Start();
        }

        /// <summary>
        /// Retrieves the user's current location and updates the map accordingly.
        /// If this is the first location update and no sensor is selected, centers the map on the user's position.
        /// For active navigation routes, updates the waypoint representing the user's position and rebuilds the route.
        /// </summary>
        private async Task GetCurrentLocationAsync()
        {
            try
            {
                var request = new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.High,
                    Timeout = TimeSpan.FromSeconds(10)
                };

                var location = await Geolocation.GetLocationAsync(request);

                if (location != null)
                {
                    bool isFirstLocation = _currentPosition == null;
                    _currentPosition = new Position(location.Longitude, location.Latitude);
                    UpdateCurrentLocationOnMap();

                    // If this is the first location fix and no sensor is selected, center the map
                    if (isFirstLocation && SelectedSensor == null)
                    {
                        var mercator = SphericalMercator.FromLonLat(_currentPosition.X, _currentPosition.Y);
                        Map.Navigator.CenterOn(mercator.x, mercator.y);
                        Map.Navigator.ZoomTo(15000);
                    }

                    // If navigating to a sensor, rebuild the route with updated location
                    if (RouteWaypoints.Count > 1 && RouteWaypoints[0].SensorId == -1)
                    {
                        // Update the current location waypoint
                        var updatedLocationSensor = new Sensor
                        {
                            SensorId = -1,
                            DisplayName = "My Location",
                            Configuration = new Configuration
                            {
                                Latitude = (float)_currentPosition.Y,
                                Longitude = (float)_currentPosition.X
                            }
                        };

                        // Replace the first waypoint with updated location
                        var waypoints = RouteWaypoints.ToList();
                        waypoints[0] = updatedLocationSensor;
                        RouteWaypoints = waypoints;

                        // Rebuild the route with updated location
                        _ = BuildRouteAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get current location");
            }
        }

        /// <summary>
        /// Updates the user's current location indicator on the map.
        /// Creates a feature with appropriate styling to represent the user's position.
        /// </summary>
        private void UpdateCurrentLocationOnMap()
        {
            if (_currentPosition == null)
                return;

            var mercator = SphericalMercator.FromLonLat(_currentPosition.X, _currentPosition.Y);

            // Create a feature for the current location with a distinctive style
            var feature = new PointFeature(mercator.x, mercator.y);

            // Add a bitmap style for the users location
            var locationStyle = new SymbolStyle
            {
                BitmapId = _statusStyles["Location"].BitmapId,
                SymbolScale = 0.2,
                SymbolOffset = new Offset(0, 0)
            };
            feature.Styles.Add(locationStyle);
            var highlightStyle = new SymbolStyle
            {
                Fill = new Mapsui.Styles.Brush { Color = Mapsui.Styles.Color.FromArgb(128, 0, 120, 255) },
                SymbolType = SymbolType.Ellipse,
                SymbolScale = 0.5
            };
            feature.Styles.Add(highlightStyle);
            // Update the location layer with the new feature
            _locationLayer.Features = new[] { feature };
            Map.Refresh();
        }

        /// <summary>
        /// Handles the refresh operation with error handling and loading state management.
        /// Acts as a safety wrapper around RefreshAsync to ensure UI states are properly maintained regardless of success or failure during the refresh operation.
        /// </summary>
        private async Task SafeRefreshAsync()
        {
            try
            {
                IsLoading = true;
                HasError = false;
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Refresh failed: {ex.Message}";
                _logger.LogError(ex, "Error refreshing sensor data");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Core refresh logic that fetches sensor data and updates the map.
        /// Uses a semaphore to prevent concurrent updates, loads sensors from service, creates map features with appropriate styles, and updates filtered lists for search results.
        /// </summary>
        private async Task RefreshAsync()
        {
            if (!await _refreshLock.WaitAsync(0)) return;

            try
            {
                // Load sensors with their configurations
                var sensors = await _sensorService.GetAllWithConfigurationAsync();
                _sensors = sensors;

                foreach (var s in sensors)
                {
                    if (string.IsNullOrEmpty(s.DisplayName))
                        s.DisplayName = $"{s.SensorType} #{s.SensorId}";
                }

                var features = new List<IFeature>(sensors.Count);

                foreach (var s in sensors)
                {
                    // Skip sensors without coordinates
                    if (s.Configuration?.Latitude.HasValue != true ||
                        s.Configuration?.Longitude.HasValue != true)
                        continue;

                    // Project to WebMercator
                    var merc = SphericalMercator.FromLonLat(
                        s.Configuration.Longitude.Value,
                        s.Configuration.Latitude.Value);
                    var pf = new PointFeature(merc.x, merc.y);
                    pf["SensorId"] = s.SensorId;    // attach ID for tap lookup

                    // Check if this is in route waypoints (selected sensor)
                    var isRoutePoint = RouteWaypoints.Any(wp => wp.SensorId == s.SensorId);
                    pf.Styles.Add(isRoutePoint ? _statusStyles["Selected"] : _statusStyles["Default"]);
                    features.Add(pf);
                }

                // Swap new features into the layer
                _pinLayer.Features = features;
                Map.Refresh();

                // Update filtered list if search is active
                if (IsSearchActive)
                {
                    FilterSensors(SearchText);
                }
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        /// <summary>
        /// Formats a TimeSpan into a human-readable string with appropriate units.
        /// Handles days, hours, minutes, and seconds with proper pluralization.
        /// </summary>
        /// <param name="span">The TimeSpan to format</param>
        /// <returns>A formatted string representation of the time span</returns>
        string FormatTimeSpan(TimeSpan span)
        {
            string formatted = string.Format("{0}{1}{2}{3}",
            span.Duration().Days > 0 ? string.Format("{0:0} day{1}, ", span.Days, span.Days == 1 ? string.Empty : "s") : string.Empty,
            span.Duration().Hours > 0 ? string.Format("{0:0} hour{1}, ", span.Hours, span.Hours == 1 ? string.Empty : "s") : string.Empty,
            span.Duration().Minutes > 0 ? string.Format("{0:0} minute{1}, ", span.Minutes, span.Minutes == 1 ? string.Empty : "s") : string.Empty,
            span.Duration().Seconds > 0 ? string.Format("{0:0} second{1}", span.Seconds, span.Seconds == 1 ? string.Empty : "s") : string.Empty);
            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);
            if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";
            return formatted;
        }

        /// <summary>
        /// Handles map information events, specifically for tapping on sensor pins.
        /// Delegates the actual handling to the HandlePinTappedAsync method.
        /// </summary>
        private void OnMapInfo(object? sender, MapInfoEventArgs e)
        {
            var info = e.MapInfo;
            if (info?.Feature == null) return;
            // Fire-and-forget on the UI thread
            _ = HandlePinTappedAsync(info);
        }

        /// <summary>
        /// Processes a tap on a sensor pin, showing information about the sensor
        /// and offering navigation options. Displays a dialog with sensor details
        /// and provides an option to navigate to the selected sensor.
        /// </summary>
        /// <param name="info">Map information from the tap event</param>
        public async Task HandlePinTappedAsync(MapInfo info)
        {
            if (info?.Feature == null) return;
            if (!(info.Feature["SensorId"] is int id)) return;
            var s = _sensors.FirstOrDefault(x => x.SensorId == id);
            if (s == null) return;

            var title = !string.IsNullOrEmpty(s.DisplayName)
                ? s.DisplayName
                : $"Sensor {s.SensorId}";

            // Format coordinates in human-readable format
            var coordinatesStr = "N/A";
            if (s.Configuration?.Latitude.HasValue == true && s.Configuration?.Longitude.HasValue == true)
            {
                coordinatesStr = $"{s.Configuration.Latitude.Value:F6}, {s.Configuration.Longitude.Value:F6}";
            }

            // Build message with sensor status and coordinates
            var msg =
                $"Status: {s.Status}\n" +
                $"Coordinates: {coordinatesStr}\n\n";

            string routeAction = "Navigate to Sensor";
            string cancelAction = "Close";

            var action = await _dialogService.DisplayConfirmationAsync(
                title, msg, routeAction, cancelAction);

            if (action)
            {
                NavigateToSensor(s);
            }
        }

        /// <summary>
        /// Registers a pin image for use on the map, loading from app resources or creating a fallback.
        /// Handles error cases by generating a colored circle when the image file cannot be found.
        /// </summary>
        /// <param name="filename">The name of the image file to load</param>
        /// <returns>The bitmap ID registered with Mapsui</returns>
        async Task<int> RegisterPinAsync(string filename)
        {
            var paths = new[] { filename, Path.Combine("Resources", "Images", filename) };
            foreach (var path in paths)
            {
                try
                {
                    using var raw = await FileSystem.OpenAppPackageFileAsync(path);
                    var ms = new MemoryStream();
                    await raw.CopyToAsync(ms);
                    ms.Position = 0;
                    var id = BitmapRegistry.Instance.Register(ms);
                    if (id > 0) { _pinStreams.Add(ms); return id; }
                    ms.Dispose();
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
            }

            // Create a default bitmap if the file wasn't found
            _logger.LogWarning("Could not find pin image '{filename}', creating default bitmap", filename);

            try
            {
                // Create a simple color bitmap based on the filename
                Microsoft.Maui.Graphics.Color color = filename.Contains("warning")
                    ? Microsoft.Maui.Graphics.Colors.Yellow  // Yellow for warning
                    : filename.Contains("location")
                        ? Microsoft.Maui.Graphics.Colors.Blue  // Blue for location
                        : Microsoft.Maui.Graphics.Colors.Green; // Green for default

                // Generate a simple image using SkiaSharp (already included in MAUI)
                var ms = new MemoryStream();
                using (var surface = SKSurface.Create(new SKImageInfo(64, 64)))
                {
                    var canvas = surface.Canvas;
                    // Clear with transparent background
                    canvas.Clear(SKColors.Transparent);

                    // Create circle with specific color
                    using var paint = new SKPaint
                    {
                        IsAntialias = true,
                        Color = new SKColor((byte)color.Red, (byte)color.Green, (byte)color.Blue, 255),
                        Style = SKPaintStyle.Fill
                    };

                    // Draw a circle
                    canvas.DrawCircle(32, 32, 28, paint);

                    // Add border
                    using var strokePaint = new SKPaint
                    {
                        IsAntialias = true,
                        Color = SKColors.White,
                        Style = SKPaintStyle.Stroke,
                        StrokeWidth = 2
                    };
                    canvas.DrawCircle(32, 32, 28, strokePaint);

                    // Convert to PNG
                    using var image = surface.Snapshot();
                    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                    data.SaveTo(ms);
                }

                ms.Position = 0;
                var id = BitmapRegistry.Instance.Register(ms);
                if (id > 0)
                {
                    _pinStreams.Add(ms);
                    return id;
                }
                ms.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create fallback bitmap for '{filename}'", filename);
            }

            _logger.LogError("Could not register pin '{filename}'", filename);
            return -1;
        }

        /// <summary>
        /// Filters the list of sensors based on search text.
        /// Matches against sensor name, type, or measurand name.
        /// Updates the FilteredSensors collection with matching sensors.
        /// </summary>
        /// <param name="searchText">The text to search for</param>
        public void FilterSensors(string searchText)
        {
            _mainThread.BeginInvokeOnMainThread(() =>
            {
                IsSearchActive = true;
                FilteredSensors.Clear();

                // If search is empty, show all sensors
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    foreach (var sensor in _sensors)
                    {
                        FilteredSensors.Add(sensor);
                    }
                    return;
                }

                // Otherwise, filter by the search text
                var lowerSearchText = searchText.ToLowerInvariant();
                var filtered = _sensors.Where(s =>
                    (s.DisplayName?.ToLowerInvariant().Contains(lowerSearchText) ?? false) ||
                    (s.SensorType?.ToLowerInvariant().Contains(lowerSearchText) ?? false) ||
                    (s.Measurand?.QuantityName?.ToLowerInvariant().Contains(lowerSearchText) ?? false)
                ).ToList();

                foreach (var sensor in filtered)
                {
                    FilteredSensors.Add(sensor);
                }
            });
        }

        /// <summary>
        /// Executes the search operation using the current search text.
        /// Called when the search button is pressed.
        /// </summary>
        private void ExecuteSearch()
        {
            FilterSensors(SearchText);
        }

        /// <summary>
        /// Hides the search results panel.
        /// Called when the user dismisses search results.
        /// </summary>
        public void HideSearchResults()
        {
            IsSearchActive = false;
        }

        /// <summary>
        /// Clears the current search text and hides search results.
        /// Called when the clear search button is pressed.
        /// </summary>
        private void ClearSearch()
        {
            SearchText = string.Empty;
            HideSearchResults();
        }

        /// <summary>
        /// Centers the map on a specific sensor location.
        /// Adjusts the zoom level for appropriate visibility.
        /// </summary>
        /// <param name="sensor">The sensor to center on</param>
        private void CenterMapOnSensor(Sensor sensor)
        {
            if (sensor?.Configuration?.Latitude == null || sensor?.Configuration?.Longitude == null)
                return;

            var merc = SphericalMercator.FromLonLat(
                sensor.Configuration.Longitude.Value,
                sensor.Configuration.Latitude.Value);

            Map.Navigator.CenterOn(merc.x, merc.y);
            Map.Navigator.ZoomTo(3000);
        }

        /// <summary>
        /// Builds a route to the specified sensor on the map using OpenRouteService.
        /// Clears any active route, adds the current location as a starting point if available.
        /// </summary>
        private void NavigateToSensor(Sensor sensor)
        {
            if (sensor == null) return;

            // Clear any existing route
            ClearRoute();

            // Add current location as start if available
            if (_currentPosition != null)
            {
                // Create a dummy sensor for the current location as a route start point
                var currentLocationSensor = new Sensor
                {
                    SensorId = -1,
                    DisplayName = "My Location",
                    Configuration = new Configuration
                    {
                        Latitude = (float)_currentPosition.Y,
                        Longitude = (float)_currentPosition.X
                    }
                };

                AddSensorToRoute(currentLocationSensor);
            }

            // Add destination sensor
            AddSensorToRoute(sensor);

            // Build route
            if (CanBuildRoute())
            {
                _ = BuildRouteAsync();
            }
        }

        /// <summary>
        /// Adds a sensor to the current navigation route.
        /// Handles special case for location-based navigation by preserving the current location waypoint.
        /// Updates the map display and command states after adding the sensor.
        /// </summary>
        /// <param name="sensor">The sensor to add to the route</param>
        private void AddSensorToRoute(Sensor sensor)
        {
            if (sensor == null) return;

            // Keep track of current location pin if it exists
            Sensor currentLocationPin = null;
            if (RouteWaypoints.Count > 0 && RouteWaypoints[0].SensorId == -1)
            {
                currentLocationPin = RouteWaypoints[0];
            }

            // Clear existing route but preserve the current location pin if it exists
            RouteWaypoints = new List<Sensor>();

            // Add back the current location pin if it existed
            if (currentLocationPin != null)
            {
                RouteWaypoints.Add(currentLocationPin);
            }

            // Don't add if it's already in route
            if (RouteWaypoints.Any(wp => wp.SensorId == sensor.SensorId))
                return;

            // Add the new sensor
            RouteWaypoints.Add(sensor);

            // Update map to show selected sensor as part of route
            _ = RefreshAsync();

            // Update commands that depend on route state
            (RemoveFromRouteCommand as Command)?.ChangeCanExecute();
            (ClearRouteCommand as Command)?.ChangeCanExecute();
            (BuildRouteCommand as Command)?.ChangeCanExecute();

            // Automatically build a route if possible
            if (CanBuildRoute())
            {
                _ = BuildRouteAsync();
            }
        }

        /// <summary>
        /// Removes a sensor from the current navigation route.
        /// Updates the map display and rebuilds the route if necessary.
        /// </summary>
        /// <param name="sensor">The sensor to remove from the route</param>
        private void RemoveSensorFromRoute(Sensor sensor)
        {
            if (sensor == null)
                return;

            var newWaypoints = RouteWaypoints.Where(wp => wp.SensorId != sensor.SensorId).ToList();
            RouteWaypoints = newWaypoints;

            // If we removed all waypoints, clear the route
            if (RouteWaypoints.Count == 0)
            {
                ClearRoute();
            }
            else
            {
                // Update map to remove this sensor from route visuals
                _ = RefreshAsync();

                // If we still have enough waypoints, rebuild the route
                if (RouteWaypoints.Count >= 2)
                {
                    _ = BuildRouteAsync();
                }
                else
                {
                    // Clear the route line if we don't have enough waypoints
                    _routeLayer.Features = Enumerable.Empty<IFeature>();
                    Map.Refresh();
                }
            }
        }

        /// <summary>
        /// Clears the current navigation route and all associated waypoints.
        /// Resets route visualization and related properties.
        /// </summary>
        private void ClearRoute()
        {
            RouteWaypoints = new List<Sensor>();
            _routeLayer.Features = Enumerable.Empty<IFeature>();
            Map.Refresh();

            // Clear route details
            RouteDistance = 0;
            RouteDuration = TimeSpan.Zero;
            RouteDetailsText = string.Empty;

            OnPropertyChanged(nameof(RouteWaypointsText));
        }

        /// <summary>
        /// Determines if a sensor can be added to the current route.
        /// A sensor can be added if it has valid coordinates and is not already in the route.
        /// </summary>
        /// <param name="sensor">The sensor to check</param>
        /// <returns>True if the sensor can be added; otherwise, false</returns>
        private bool CanAddToRoute(Sensor sensor)
        {
            // Can add if sensor has coordinates and is not already in route
            return sensor?.Configuration?.Latitude != null &&
                   sensor?.Configuration?.Longitude != null &&
                   !RouteWaypoints.Any(wp => wp.SensorId == sensor.SensorId);
        }

        /// <summary>
        /// Determines if a sensor can be removed from the current route.
        /// A sensor can be removed if it is currently in the route.
        /// </summary>
        /// <param name="sensor">The sensor to check</param>
        /// <returns>True if the sensor can be removed; otherwise, false</returns>
        private bool CanRemoveFromRoute(Sensor sensor)
        {
            // Can remove if sensor is in route
            return sensor != null && RouteWaypoints.Any(wp => wp.SensorId == sensor.SensorId);
        }

        /// <summary>
        /// Determines if a route can be built with the current waypoints.
        /// A route can be built if there are at least 2 waypoints.
        /// </summary>
        /// <returns>True if a route can be built; otherwise, false</returns>
        private bool CanBuildRoute()
        {
            // Need at least 2 waypoints to build a route
            return RouteWaypoints.Count >= 2;
        }

        /// <summary>
        /// Builds a route between waypoints using the OpenRouteService API.
        /// Handles coordinate preparation, API requests, route rendering on the map, and displays route information (distance and duration).
        /// </summary>
        private async Task BuildRouteAsync()
        {
            if (RouteWaypoints.Count < 2)
            {
                await _dialogService.DisplayAlertAsync("Route Building",
                    "At least two waypoints are needed to build a route.", "OK");
                return;
            }

            try
            {
                IsLoading = true;

                // Prepare coordinates for OpenRouteService
                var coordinates = new List<double[]>();

                foreach (var waypoint in RouteWaypoints)
                {
                    if (waypoint.Configuration?.Latitude.HasValue != true ||
                        waypoint.Configuration?.Longitude.HasValue != true)
                        continue;

                    coordinates.Add(new double[]
                    {
                waypoint.Configuration.Longitude.Value,
                waypoint.Configuration.Latitude.Value
                    });
                }

                if (coordinates.Count < 2)
                {
                    await _dialogService.DisplayAlertAsync("Route Error",
                        "Not enough valid coordinates to build a route.", "OK");
                    return;
                }

                // Get profile name from selected travel mode
                string profile = _travelModeProfiles[SelectedTravelMode];

                // Create the request for OpenRouteService
                var routeRequest = new
                {
                    coordinates,
                    format = "geojson",
                    profile = profile,
                    preference = "recommended",
                    units = "m",
                    instructions = true,
                    language = "en"
                };

                // Call the OpenRouteService API
                var response = await _httpClient.PostAsJsonAsync(
                    $"v2/directions/{profile}/geojson", routeRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"OpenRouteService API error: {response.StatusCode}, {errorContent}");
                }

                // Parse the GeoJSON response
                var routeGeoJson = await response.Content.ReadFromJsonAsync<GeoJsonFeatureCollection>();

                if (routeGeoJson?.features == null || !routeGeoJson.features.Any())
                    throw new Exception("No route found");

                // Extract the route geometry and summary
                var route = routeGeoJson.features.First();
                var routeGeometry = route.geometry;
                var summary = route.properties?.summary;

                // Create Mapsui line geometry from the GeoJSON coordinates
                var routeFeatures = new List<IFeature>();
                var routeCoordinates = new List<Mapsui.MPoint>();

                foreach (var coordinate in routeGeometry.coordinates)
                {
                    if (coordinate.Length >= 2)
                    {
                        var mercator = SphericalMercator.FromLonLat(coordinate[0], coordinate[1]);
                        routeCoordinates.Add(new Mapsui.MPoint(mercator.x, mercator.y));
                    }
                }

                if (routeCoordinates.Count < 2)
                    throw new Exception("Invalid route geometry");

                // Convert to NetTopologySuite Coordinates
                var ntsCoordinates = routeCoordinates
                    .Select(p => new NetTopologySuite.Geometries.Coordinate(p.X, p.Y))
                    .ToArray();

                // Create a LineString feature
                var lineString = new NetTopologySuite.Geometries.LineString(ntsCoordinates);
                var routeFeature = new Mapsui.Nts.GeometryFeature(lineString);

                // Add metadata
                routeFeature["distance"] = summary?.distance ?? 0;
                routeFeature["duration"] = summary?.duration ?? 0;

                // Add the route to the route layer
                _routeLayer.Features = new List<IFeature> { routeFeature };
                Map.Refresh();

                // Zoom to show the entire route
                if (routeCoordinates.Count > 0)
                {
                    // Create a bounding box from the route points
                    double minX = routeCoordinates.Min(p => p.X);
                    double minY = routeCoordinates.Min(p => p.Y);
                    double maxX = routeCoordinates.Max(p => p.X);
                    double maxY = routeCoordinates.Max(p => p.Y);

                    var extent = new Mapsui.MRect(minX, minY, maxX, maxY);

                    // Add some padding
                    double padding = extent.Width * 0.2;
                    extent = new Mapsui.MRect(
                        extent.Min.X - padding,
                        extent.Min.Y - padding,
                        extent.Max.X + padding,
                        extent.Max.Y + padding);

                    Map.Navigator.ZoomToBox(extent);
                }

                // Display route information
                RouteDistance = (summary?.distance ?? 0) / 1000.0; // Convert to km
                RouteDuration = TimeSpan.FromSeconds(summary?.duration ?? 0);
                UpdateRouteDetailsText();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build route");
                await _dialogService.DisplayErrorAsync($"Failed to build route: {ex.Message}");

                // Clear the route layer
                _routeLayer.Features = Enumerable.Empty<IFeature>();
                Map.Refresh();
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Safely refreshes both location data and sensor data with error handling.
        /// Called periodically by the refresh timer to keep the map up to date.
        /// </summary>
        private async Task SafeRefreshLocationAndDataAsync()
        {
            try
            {
                // Refresh user location
                await GetCurrentLocationAsync();

                // Then refresh sensor data
                await SafeRefreshAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during automatic refresh");
            }
        }

        /// <summary>
        /// Implements IDisposable to clean up resources used by the view model.
        /// Stops timers, unregisters event handlers, and disposes streams.
        /// </summary>
        public void Dispose()
        {
            _refreshTimer?.Stop();
            Map.Info -= OnMapInfo;
            _refreshLock.Dispose();
            foreach (var ms in _pinStreams)
            {
                ms.Dispose();
            }
            _pinStreams.Clear();
            _httpClient.Dispose();
        }
    }

    /// <summary>
    /// Represents a collection of GeoJSON features from the OpenRouteService API.
    /// </summary>
    public class GeoJsonFeatureCollection
    {
        /// <summary>
        /// The GeoJSON type, typically "FeatureCollection".
        /// </summary>
        public string type { get; set; }

        /// <summary>
        /// The list of features contained in the collection.
        /// </summary>
        public List<GeoJsonFeature> features { get; set; }
    }

    /// <summary>
    /// Represents a single GeoJSON feature with properties and geometry.
    /// </summary>
    public class GeoJsonFeature
    {
        /// <summary>
        /// The GeoJSON feature type, typically "Feature".
        /// </summary>
        public string type { get; set; }

        /// <summary>
        /// Properties associated with the feature, including route information.
        /// </summary>
        public GeoJsonProperties properties { get; set; }

        /// <summary>
        /// The geometry that defines the feature's shape and location.
        /// </summary>
        public GeoJsonGeometry geometry { get; set; }
    }

    /// <summary>
    /// Represents properties associated with a GeoJSON feature.
    /// Contains route information such as summary and segments.
    /// </summary>
    public class GeoJsonProperties
    {
        /// <summary>
        /// Summary information about the route, including distance and duration.
        /// </summary>
        public GeoJsonSummary summary { get; set; }

        /// <summary>
        /// Detailed information about the route's segments.
        /// </summary>
        public List<GeoJsonSegment> segments { get; set; }
    }

    /// <summary>
    /// Represents summary information for a route, including total distance and duration.
    /// </summary>
    public class GeoJsonSummary
    {
        /// <summary>
        /// The total distance of the route in meters.
        /// </summary>
        public double distance { get; set; }

        /// <summary>
        /// The total duration of the route in seconds.
        /// </summary>
        public double duration { get; set; }
    }

    /// <summary>
    /// Represents a segment of a route with distance, duration, and steps.
    /// </summary>
    public class GeoJsonSegment
    {
        /// <summary>
        /// The distance of this segment in meters.
        /// </summary>
        public double distance { get; set; }

        /// <summary>
        /// The duration of this segment in seconds.
        /// </summary>
        public double duration { get; set; }

        /// <summary>
        /// The individual steps that make up this segment.
        /// </summary>
        public List<GeoJsonStep> steps { get; set; }
    }

    /// <summary>
    /// Represents a single navigation step within a route segment.
    /// </summary>
    public class GeoJsonStep
    {
        /// <summary>
        /// The distance of this step in meters.
        /// </summary>
        public double distance { get; set; }

        /// <summary>
        /// The duration of this step in seconds.
        /// </summary>
        public double duration { get; set; }

        /// <summary>
        /// The navigation instruction for this step (e.g., "Turn right").
        /// </summary>
        public string instruction { get; set; }

        /// <summary>
        /// The name of the road or path for this step.
        /// </summary>
        public string name { get; set; }
    }

    /// <summary>
    /// Represents the geometry component of a GeoJSON feature.
    /// </summary>
    public class GeoJsonGeometry
    {
        /// <summary>
        /// The GeoJSON geometry type, typically "LineString" for routes.
        /// </summary>
        public string type { get; set; }

        /// <summary>
        /// The array of coordinate pairs that define the geometry.
        /// For routes, this is a sequence of [longitude, latitude] points.
        /// </summary>
        public List<double[]> coordinates { get; set; }
    }

    /// <summary>
    /// Represents a geographical position with X (longitude) and Y (latitude) coordinates.
    /// Used for tracking and displaying the user's current location.
    /// </summary>
    public class Position
    {
        /// <summary>
        /// Gets or sets the X coordinate (longitude).
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Gets or sets the Y coordinate (latitude).
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Initializes a new instance of the Position class with the specified coordinates.
        /// </summary>
        /// <param name="x">The X coordinate (longitude)</param>
        /// <param name="y">The Y coordinate (latitude)</param>
        public Position(double x, double y)
        {
            X = x;
            Y = y;
        }
    }


}
