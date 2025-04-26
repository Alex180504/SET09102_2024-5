using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.Logging;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using SET09102_2024_5.Services;
using Map = Mapsui.Map;
using MPoint = Mapsui.MPoint;
using Mapsui.Projections;

namespace SET09102_2024_5.ViewModels
{
    public class MapViewModel : IDisposable
    {
        public Map Map { get; }

        // Layer that holds our default‐pin features
        private readonly MemoryLayer _pinLayer;

        // The one style we’ll use for every sensor
        private SymbolStyle _defaultStyle;

        private readonly SensorService _sensorService;
        private readonly IMainThreadService _mainThread;
        private readonly ILogger<MapViewModel> _logger;

        // Prevent overlapping refreshes
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        // Keep the pin’s MemoryStream alive
        private readonly List<Stream> _pinStreams = new();

        public MapViewModel(
            SensorService sensorService,
            IMainThreadService mainThread,
            ILogger<MapViewModel> logger)
        {
            _sensorService = sensorService;
            _mainThread = mainThread;
            _logger = logger;

            // 1) Create the map + OSM base layer
            Map = new Map();
            Map.Layers.Add(OpenStreetMap.CreateTileLayer());

            // 2) Prepare an empty pin layer
            _pinLayer = new MemoryLayer("Pins")
            {
                Features = Enumerable.Empty<IFeature>(),
                IsMapInfoLayer = true
            };
            Map.Layers.Add(_pinLayer);
        }

        /// <summary>
        /// Call from your page’s OnAppearing(): registers the default pin,
        /// does an initial draw of all sensors, subscribes to updates,
        /// and kicks off polling.
        /// </summary>
        public async Task InitializeAsync()
        {
            // Register the single default‐pin image
            var defaultPinId = await RegisterDefaultPinAsync("pin_default.png");
            if (defaultPinId <= 0)
                throw new InvalidOperationException("Failed to register default pin.");

            // Cache the style (smaller scale so it’s not huge)
            _defaultStyle = new SymbolStyle
            {
                BitmapId = defaultPinId,
                SymbolScale = 0.2
            };

            // Subscribe to updates
            _sensorService.OnSensorUpdated += OnSensorUpdated;

            // Initial draw
            await RefreshAsync();

            // Start the service’s polling loop (will fire OnSensorUpdated)
            _ = _sensorService.StartAsync(TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Use MAUI assets API to open the PNG, copy to a seekable MemoryStream,
        /// register that with Mapsui, and keep it alive.
        /// </summary>
        private async Task<int> RegisterDefaultPinAsync(string fileName)
        {
            var tryPaths = new[] { fileName, Path.Combine("Resources", "Images", fileName) };
            foreach (var path in tryPaths)
            {
                try
                {
                    using var raw = await FileSystem.OpenAppPackageFileAsync(path);
                    var ms = new MemoryStream();
                    await raw.CopyToAsync(ms);
                    ms.Position = 0;
                    var id = BitmapRegistry.Instance.Register(ms);
                    if (id > 0)
                    {
                        _pinStreams.Add(ms);
                        _logger.LogInformation("Registered default pin from '{path}' = {id}", path, id);
                        return id;
                    }
                    ms.Dispose();
                }
                catch (FileNotFoundException)
                {
                    _logger.LogWarning("Default pin not found at '{path}'", path);
                }
            }
            _logger.LogError("Could not register default pin; tried: {paths}", string.Join(", ", tryPaths));
            return -1;
        }

        private void OnSensorUpdated(Sensor _, DateTime? __)
        {
            // Marshal to UI thread, then refresh
            _mainThread.BeginInvokeOnMainThread(async () => await RefreshAsync());
        }

        private async Task RefreshAsync()
        {
            if (!await _refreshLock.WaitAsync(0))
                return;

            try
            {
                // Get all sensors with their configuration
                var sensors = await _sensorService.GetAllWithConfigurationAsync();

                // Build a pin (point feature) per sensor
                var features = sensors
                    .Where(s => s.Configuration?.Latitude.HasValue == true &&
                                s.Configuration?.Longitude.HasValue == true)
                    .Select(s =>
                    {
                        var lon = s.Configuration.Longitude.Value;
                        var lat = s.Configuration.Latitude.Value;
                        var merc = SphericalMercator.FromLonLat(lon, lat);
                        var pf = new PointFeature(merc.x, merc.y);
                        pf.Styles.Add(_defaultStyle);
                        return (IFeature)pf;
                    })
                    .ToList();
                // Swap into the layer
                _pinLayer.Features = features;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh pin layer");
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        /// <summary>
        /// Stops listening to sensor updates but leaves the VM intact so
        /// you can re-call InitializeAsync later if you need.
        /// </summary>
        public void Stop()
        {
            _sensorService.OnSensorUpdated -= OnSensorUpdated;
        }

        public void Dispose()
        {
            // Unsubscribe & clean up
            _sensorService.OnSensorUpdated -= OnSensorUpdated;
            _refreshLock.Dispose();
            foreach (var s in _pinStreams) s.Dispose();
            _pinStreams.Clear();
        }
    }
}
