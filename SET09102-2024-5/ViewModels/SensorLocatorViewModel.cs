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
    public enum TravelMode
    {
        Walking,
        Driving
    }

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


        public Map Map { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand AddToRouteCommand { get; }
        public ICommand RemoveFromRouteCommand { get; }
        public ICommand ClearRouteCommand { get; }
        public ICommand BuildRouteCommand { get; }
        public ICommand NavigateToSensorCommand { get; }
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

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool HasError
        {
            get => _hasError;
            private set => SetProperty(ref _hasError, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public ObservableCollection<Sensor> FilteredSensors
        {
            get => _filteredSensors;
            set => SetProperty(ref _filteredSensors, value);
        }

        public bool IsSearchActive
        {
            get => _isSearching;
            set => SetProperty(ref _isSearching, value);
        }

        public bool IsRouteBuilding
        {
            get => _isRouteBuilding;
            set => SetProperty(ref _isRouteBuilding, value);
        }

        public double RouteDistance
        {
            get => _routeDistance;
            set => SetProperty(ref _routeDistance, value);
        }

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

        public string RouteDetailsText
        {
            get => _routeDetailsText;
            private set => SetProperty(ref _routeDetailsText, value);
        }

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

        public List<TravelMode> AvailableTravelModes => Enum.GetValues<TravelMode>().ToList();

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

        private void OnMapInfo(object? sender, MapInfoEventArgs e)
        {
            var info = e.MapInfo;
            if (info?.Feature == null) return;
            // Fire-and-forget on the UI thread
            _ = HandlePinTappedAsync(info);
        }

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

        private void ExecuteSearch()
        {
            FilterSensors(SearchText);
        }

        public void HideSearchResults()
        {
            IsSearchActive = false;
        }

        private void ClearSearch()
        {
            SearchText = string.Empty;
            HideSearchResults();
        }

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

        private bool CanAddToRoute(Sensor sensor)
        {
            // Can add if sensor has coordinates and is not already in route
            return sensor?.Configuration?.Latitude != null &&
                   sensor?.Configuration?.Longitude != null &&
                   !RouteWaypoints.Any(wp => wp.SensorId == sensor.SensorId);
        }

        private bool CanRemoveFromRoute(Sensor sensor)
        {
            // Can remove if sensor is in route
            return sensor != null && RouteWaypoints.Any(wp => wp.SensorId == sensor.SensorId);
        }

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

    // Helper classes for OpenRouteService API
    public class GeoJsonFeatureCollection
    {
        public string type { get; set; }
        public List<GeoJsonFeature> features { get; set; }
    }

    public class GeoJsonFeature
    {
        public string type { get; set; }
        public GeoJsonProperties properties { get; set; }
        public GeoJsonGeometry geometry { get; set; }
    }

    public class GeoJsonProperties
    {
        public GeoJsonSummary summary { get; set; }
        public List<GeoJsonSegment> segments { get; set; }
    }

    public class GeoJsonSummary
    {
        public double distance { get; set; }
        public double duration { get; set; }
    }

    public class GeoJsonSegment
    {
        public double distance { get; set; }
        public double duration { get; set; }
        public List<GeoJsonStep> steps { get; set; }
    }

    public class GeoJsonStep
    {
        public double distance { get; set; }
        public double duration { get; set; }
        public string instruction { get; set; }
        public string name { get; set; }
    }

    public class GeoJsonGeometry
    {
        public string type { get; set; }
        public List<double[]> coordinates { get; set; }
    }

    public class Position
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Position(double x, double y)
        {
            X = x;
            Y = y;
        }
    }


}
