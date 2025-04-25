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
using Microsoft.Maui.Storage;            // FileSystem.OpenAppPackageFileAsync
using Microsoft.Extensions.Logging;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using SET09102_2024_5.Services;
using Map = Mapsui.Map;
using MPoint = Mapsui.MPoint;

namespace SET09102_2024_5.ViewModels
{
    public class MapViewModel : IDisposable
    {
        public Map Map { get; }

        // A dedicated layer just for pins
        private readonly MemoryLayer _pinLayer;

        // Default pin style
        private SymbolStyle _defaultStyle;

        private readonly SensorService _sensorService;
        private readonly IMainThreadService _mainThread;
        private readonly ILogger<MapViewModel> _logger;

        // Prevent overlapping RefreshAsync calls
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        // Keep the default‐pin stream alive until Dispose()
        private readonly List<Stream> _pinStreams = new();

        public MapViewModel(
            SensorService sensorService,
            IMainThreadService mainThread,
            ILogger<MapViewModel> logger)
        {
            _sensorService = sensorService;
            _mainThread = mainThread;
            _logger = logger;

            // 1) Create the Map + base OSM layer
            Map = new Map();
            Map.Layers.Add(OpenStreetMap.CreateTileLayer());

            // 2) Prepare an empty pin layer (no style here; each feature has its own)
            _pinLayer = new MemoryLayer("Pins")
            {
                Features = Enumerable.Empty<IFeature>()
            };
            Map.Layers.Add(_pinLayer);
        }

        /// <summary>
        /// Call from your page’s OnAppearing(): registers the default pin,
        /// does an initial draw, subscribes to updates, and starts polling.
        /// </summary>
        public async Task InitializeAsync()
        {
            // 1) Load & register the default pin PNG
            var defaultPinId = await RegisterDefaultPinAsync("pin_default.png");
            if (defaultPinId <= 0)
                throw new InvalidOperationException("Failed to register default pin.");

            // 2) Cache the default pin style (smaller scale so it isn’t huge)
            _defaultStyle = new SymbolStyle
            {
                BitmapId = defaultPinId,
                SymbolScale = 0.2
            };

            // 3) First draw and subscribe to sensor updates
            _sensorService.OnSensorUpdated += OnSensorUpdated;
            await RefreshAsync();

            // 4) Start built-in polling (fires OnSensorUpdated)
            _ = _sensorService.StartAsync(TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Opens the MAUI asset, copies it into a MemoryStream,
        /// registers it, and keeps the stream alive.
        /// </summary>
        private async Task<int> RegisterDefaultPinAsync(string fileName)
        {
            // Try bare filename, then under Resources/Images/
            var tryPaths = new[]
            {
        fileName,
        Path.Combine("Resources", "Images", fileName)
    };

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
                        _pinStreams.Add(ms);   // keep the stream alive
                        _logger.LogInformation($"Registered pin from '{path}' = ID {id}");
                        return id;
                    }
                    _logger.LogWarning($"BitmapRegistry.Register returned {id} for '{path}'");
                    ms.Dispose();
                }
                catch (FileNotFoundException)
                {
                    _logger.LogWarning($"Pin asset not found at '{path}'");
                }
            }

            _logger.LogError($"Could not find '{fileName}' in any of: {string.Join(", ", tryPaths)}");
            return -1;
        }

        private void OnSensorUpdated(Sensor _, DateTime? __)
        {
            // Ensure only one RefreshAsync is running at once
            _mainThread.BeginInvokeOnMainThread(async () => await RefreshAsync());
        }

        private async Task RefreshAsync()
        {
            if (!await _refreshLock.WaitAsync(0))
                return;

            try
            {
                var sensors = await _sensorService.GetAllWithConfigurationAsync();

                // Build one feature per sensor, all using the same default style
                var features = sensors
                    .Where(s => s.Configuration?.Latitude.HasValue == true
                             && s.Configuration?.Longitude.HasValue == true)
                    .Select(s =>
                    {
                        var lon = s.Configuration.Longitude.Value;
                        var lat = s.Configuration.Latitude.Value;

                        var pf = new PointFeature(new MPoint(lon, lat));
                        pf.Styles.Add(_defaultStyle);
                        return (IFeature)pf;
                    })
                    .ToList();

                // Swap into the pin layer
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
        /// Stops live updates but leaves the internal semaphore & streams intact
        /// so InitializeAsync can be called again later.
        /// </summary>
        public void Stop()
        {
            _sensorService.OnSensorUpdated -= OnSensorUpdated;
            // don’t dispose _refreshLock or streams here
        }

        public void Dispose()
        {
            _sensorService.OnSensorUpdated -= OnSensorUpdated;
            _refreshLock.Dispose();
            foreach (var s in _pinStreams) s.Dispose();
            _pinStreams.Clear();
        }
    }
}
