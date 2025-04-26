using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Styles;
using Mapsui.Projections;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.Logging;
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

        private readonly MemoryLayer _pinLayer;
        private readonly SensorService _sensorService;
        private readonly IMainThreadService _mainThread;
        private readonly IMeasurementRepository _measurementRepo;
        private readonly IDialogService _dialogService;
        private readonly ILogger<MapViewModel> _logger;

        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private readonly List<Stream> _pinStreams = new();
        private readonly Dictionary<string, SymbolStyle> _statusStyles = new();
        private List<Sensor> _currentSensors = new();

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

            // Initialize the map and add OSM base layer
            Map = new Map();
            Map.Layers.Add(OpenStreetMap.CreateTileLayer());

            // Prepare an empty layer for our pins
            _pinLayer = new MemoryLayer("Pins")
            {
                Features = Enumerable.Empty<IFeature>(),
                IsMapInfoLayer = true,
                Style = null // supresses default white circle
            };
            Map.Layers.Add(_pinLayer);

            // Hook up the map‐tap event
            Map.Info += OnMapInfo;
        }

        public async Task InitializeAsync()
        {
            // Register each pin image and build status styles
            var okId = await RegisterPinAsync("pin_default.png");
            var warnId = await RegisterPinAsync("pin_warning.png");
            var neutralId = await RegisterPinAsync("pin_neutral.png");

            _statusStyles["Active"] = new SymbolStyle { BitmapId = okId, SymbolScale = 0.1 };
            _statusStyles["Warning"] = new SymbolStyle { BitmapId = warnId, SymbolScale = 0.1 };
            _statusStyles["Inactive"] = new SymbolStyle { BitmapId = neutralId, SymbolScale = 0.1 };
            _statusStyles["Maintenance"] = _statusStyles["Inactive"];

            // Subscribe to sensor updates, draw initial pins, start polling
            _sensorService.OnSensorUpdated += OnSensorUpdated;
            await RefreshAsync();
            _ = _sensorService.StartAsync(TimeSpan.FromSeconds(5));
        }

        private void OnSensorUpdated(Sensor _, DateTime? __) =>
            _mainThread.BeginInvokeOnMainThread(async () => await RefreshAsync());

        /// Rebuilds the pin layer, choosing a warning style when needed.
        private async Task RefreshAsync()
        {
            if (!await _refreshLock.WaitAsync(0)) return;      // skip if already refreshing

            try
            {
                // Load sensors with their configurations
                var sensors = await _sensorService.GetAllWithConfigurationAsync();
                _currentSensors = sensors;

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
                    pf["SensorId"] = s.SensorId;                  // attach ID for tap lookup

                    // Get latest reading and determine if it’s a warning
                    var lastReading = await _measurementRepo.GetLatestForSensorAsync(s.SensorId);
                    bool isWarning = CheckWarning(s, lastReading);

                    // Choose style: warning trumps status
                    pf.Styles.Add(isWarning
                        ? _statusStyles["Warning"]
                        : (_statusStyles.TryGetValue(s.Status, out var st)
                            ? st
                            : _statusStyles["Active"]));

                    features.Add(pf);
                }

                // Swap new features into the layer
                _pinLayer.Features = features;
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

        /// Encapsulates stale or out‐of‐threshold logic for a sensor.
        private bool CheckWarning(Sensor sensor, MeasurementDto? last)
        {
            if (last == null) return false;

            // Staleness: age > half of configured frequency
            var freq = sensor.Configuration?.MeasurementFrequency ?? 0;
            if (last.Timestamp.HasValue && freq > 0)
            {
                var age = DateTime.UtcNow - last.Timestamp.Value;
                if (age > TimeSpan.FromMinutes(freq / 2.0))
                    return true;
            }

            // Threshold breach
            var min = sensor.Configuration?.MinThreshold;
            var max = sensor.Configuration?.MaxThreshold;
            if (last.Value.HasValue && min.HasValue && max.HasValue)
            {
                if (last.Value < min || last.Value > max)
                    return true;
            }

            return false;
        }

        private void OnMapInfo(object? sender, MapInfoEventArgs e)
        {
            _ = HandlePinTappedAsync(e.MapInfo);
        }

        public async Task HandlePinTappedAsync(MapInfo info)
        {
            if (!(info.Feature["SensorId"] is int sensorId)) return;
            var sensor = _currentSensors.FirstOrDefault(s => s.SensorId == sensorId);
            if (sensor == null) return;

            var last = await _measurementRepo.GetLatestForSensorAsync(sensorId);
            bool warning = CheckWarning(sensor, last);

            var msg =
                $"Status: {sensor.Status}\n" +
                $"Last reading: {last?.Value?.ToString() ?? "N/A"}\n" +
                $"At: {last?.Timestamp?.ToString("g") ?? "N/A"}\n\n" +
                (warning ? "⚠️ Warning!" : "All readings OK");

            await _dialogService.DisplayAlertAsync($"Sensor {sensorId}", msg, "OK");
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
            }
            _logger.LogError("Could not register pin '{filename}'", filename);
            return -1;
        }

        public void Stop() =>
            _sensorService.OnSensorUpdated -= OnSensorUpdated;

        public void Dispose()
        {
            Stop();
            Map.Info -= OnMapInfo;
            _refreshLock.Dispose();
            foreach (var ms in _pinStreams) ms.Dispose();
            _pinStreams.Clear();
        }
    }
}
