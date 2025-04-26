using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mapsui;                         // Map, MapInfo
using Mapsui.Layers;                 // MemoryLayer
using Mapsui.Styles;                 // SymbolStyle
using Mapsui.Projections;            // SphericalMercator
using Microsoft.Maui.Storage;        // FileSystem
using Microsoft.Extensions.Logging;  // ILogger
using SET09102_2024_5.Data.Repositories;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using Mapsui.Tiling;
using Map = Mapsui.Map;
using SET09102_2024_5.Services;

namespace SET09102_2024_5.ViewModels
{
    public class MapViewModel : IDisposable
    {
        public Map Map { get; }

        readonly MemoryLayer _pinLayer;
        readonly SensorService _sensorService;
        readonly IMainThreadService _mainThread;
        readonly IMeasurementRepository _measurementRepo;
        readonly IDialogService _dialogService;
        readonly ILogger<MapViewModel> _logger;

        readonly SemaphoreSlim _refreshLock = new(1, 1);
        readonly List<Stream> _pinStreams = new();
        readonly Dictionary<string, SymbolStyle> _statusStyles = new();
        List<Sensor> _currentSensors = new();

        public MapViewModel(
            SensorService sensorService,
            IMainThreadService mainThread,
            IMeasurementRepository measurementRepo,
            IDialogService dialogService,
            ILogger<MapViewModel> logger)
        {
            _sensorService = sensorService;
            _mainThread = mainThread;
            _measurementRepo = measurementRepo;
            _dialogService = dialogService;
            _logger = logger;

            // 1) Create the Map & base layer
            Map = new Map();
            Map.Layers.Add(OpenStreetMap.CreateTileLayer());

            // 2) Add a MemoryLayer for pins
            _pinLayer = new MemoryLayer("Pins")
            {
                Features = Enumerable.Empty<IFeature>(),
                IsMapInfoLayer = true,
                Style = null
            };
            Map.Layers.Add(_pinLayer);

            // 3) Listen for taps on the Map
            Map.Info += OnMapInfo;
        }

        /// <summary>
        /// Call from the page's OnAppearing.
        /// </summary>
        public async Task InitializeAsync()
        {
            // Register your PNGs
            var okId = await RegisterPinAsync("pin_default.png");
            var warnId = await RegisterPinAsync("pin_warning.png");
            var neutralId = await RegisterPinAsync("pin_neutral.png");

            // Build a SymbolStyle per status
            _statusStyles["Active"] = new SymbolStyle { BitmapId = okId, SymbolScale = 0.1 };
            _statusStyles["Warning"] = new SymbolStyle { BitmapId = warnId, SymbolScale = 0.1 };
            _statusStyles["Inactive"] = new SymbolStyle { BitmapId = neutralId, SymbolScale = 0.1 };
            _statusStyles["Maintenance"] = _statusStyles["Inactive"];

            // Subscribe & initial draw & start polling
            _sensorService.OnSensorUpdated += OnSensorUpdated;
            await RefreshAsync();
            _ = _sensorService.StartAsync(TimeSpan.FromSeconds(5));
        }

        void OnSensorUpdated(Sensor _, DateTime? __) =>
            _mainThread.BeginInvokeOnMainThread(async () => await RefreshAsync());

        /// <summary>
        /// Rebuilds all pin features from the latest sensor data.
        /// </summary>
        async Task RefreshAsync()
        {
            if (!await _refreshLock.WaitAsync(0)) return;
            try
            {
                var sensors = await _sensorService.GetAllWithConfigurationAsync();
                _currentSensors = sensors;

                var feats = sensors
                    .Where(s => s.Configuration?.Latitude.HasValue == true &&
                                s.Configuration?.Longitude.HasValue == true)
                    .Select(s =>
                    {
                        var merc = SphericalMercator.FromLonLat(
                            s.Configuration.Longitude.Value,
                            s.Configuration.Latitude.Value);

                        var pf = new PointFeature(merc.x, merc.y);
                        // Store sensor ID in the built-in indexer
                        pf["SensorId"] = s.SensorId;

                        // Pick style by status (fallback to Active)
                        if (_statusStyles.TryGetValue(s.Status, out var style))
                            pf.Styles.Add(style);
                        else
                            pf.Styles.Add(_statusStyles["Active"]);

                        return (IFeature)pf;
                    })
                    .ToList();

                _pinLayer.Features = feats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh map pins");
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        /// <summary>
        /// Fired whenever the user taps the map.
        /// </summary>
        private void OnMapInfo(object? sender, MapInfoEventArgs e)
        {
            var info = e.MapInfo;
            if (info?.Feature == null) return;
            // Fire-and-forget on the UI thread
            _ = HandlePinTappedAsync(info);
        }

        /// <summary>
        /// Shows the sensor’s status, last reading, timestamp, and any warning.
        /// </summary>
        public async Task HandlePinTappedAsync(MapInfo info)
        {
            if (!(info.Feature["SensorId"] is int sensorId)) return;
            var sensor = _currentSensors.FirstOrDefault(s => s.SensorId == sensorId);
            if (sensor == null) return;

            // Get latest measurement
            var all = await _measurementRepo.FindAsync(m => m.SensorId == sensorId);
            var last = all
                .Where(m => m.Timestamp.HasValue)
                .OrderByDescending(m => m.Timestamp.Value)
                .FirstOrDefault();

            // Check staleness & thresholds
            var freq = sensor.Configuration?.MeasurementFrequency ?? 0;
            var min = sensor.Configuration?.MinThreshold;
            var max = sensor.Configuration?.MaxThreshold;
            var now = DateTime.UtcNow;
            var warning = false;

            if (last?.Timestamp.HasValue == true && freq > 0 &&
                now - last.Timestamp.Value > TimeSpan.FromMinutes(freq / 2.0))
                warning = true;

            if (last?.Value.HasValue == true && min.HasValue && max.HasValue &&
                (last.Value < min || last.Value > max))
                warning = true;

            // Build message
            var msg =
                $"Status: {sensor.Status}\n" +
                $"Last reading: {last?.Value?.ToString() ?? "N/A"}\n" +
                $"At: {last?.Timestamp?.ToString("g") ?? "N/A"}\n\n" +
                (warning
                    ? "⚠️ Warning: stale or out-of-threshold reading"
                    : "All readings OK");

            await _dialogService.DisplayAlertAsync($"Sensor {sensorId}", msg, "OK");
        }

        /// <summary>
        /// Helper to register a PNG in your app bundle with Mapsui.
        /// </summary>
        async Task<int> RegisterPinAsync(string filename)
        {
            var tryPaths = new[] { filename, Path.Combine("Resources", "Images", filename) };
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
                        return id;
                    }
                    ms.Dispose();
                }
                catch (FileNotFoundException) { /* try next */ }
            }
            _logger.LogError("Could not register pin image {filename}", filename);
            return -1;
        }

        public void Stop() =>
            _sensorService.OnSensorUpdated -= OnSensorUpdated;

        public void Dispose()
        {
            Stop();
            _refreshLock.Dispose();
            foreach (var ms in _pinStreams) ms.Dispose();
            _pinStreams.Clear();
            // Unsubscribe Map.Info
            Map.Info -= OnMapInfo;
        }
    }
}
