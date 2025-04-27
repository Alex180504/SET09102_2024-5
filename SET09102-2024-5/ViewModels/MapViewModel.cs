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

                    // get latest reading
                    var last = await _measurementRepo.GetLatestForSensorAsync(s.SensorId);

                    // compute warning reason
                    var reason = GetWarningReason(s, last);

                    // choose style: warning overrides status
                    pf.Styles.Add(
                        reason != null
                        ? _statusStyles["Warning"]
                        : (_statusStyles.TryGetValue(s.Status, out var st) ? st : _statusStyles["Active"])
                    );

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
        string? GetWarningReason(Sensor s, MeasurementDto? last)
        {
            if (last == null) return null;

            // 1) Check staleness
            var freq = s.Configuration?.MeasurementFrequency ?? 0;
            if (last.Timestamp.HasValue && freq > 0)
            {
                var age = DateTime.UtcNow - last.Timestamp.Value;
                var threshold = TimeSpan.FromMinutes(freq / 2.0);
                if (age > threshold)
                    return $"reading is late by {FormatTimeSpan(age - threshold)}";
            }

            // 2) Check min/max thresholds
            var min = s.Configuration?.MinThreshold;
            var max = s.Configuration?.MaxThreshold;
            if (last.Value.HasValue && min.HasValue && max.HasValue)
            {
                if (last.Value < min)
                    return $"reading {last.Value} below minimum by {min - last.Value} {s.Measurand.Unit}";
                if (last.Value > max)
                    return $"reading {last.Value} above maximum by {last.Value - max} {s.Measurand.Unit}";
            }

            return null;
        }

        // formats e.g. "5m", "1h 15m"
        string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            if (ts.TotalMinutes >= 1)
                return $"{(int)ts.TotalMinutes}m";
            return $"{ts.Seconds}s";
        }

        private void OnMapInfo(object? sender, MapInfoEventArgs e)
        {
            _ = HandlePinTappedAsync(e.MapInfo);
        }

        public async Task HandlePinTappedAsync(MapInfo info)
        {
            if (!(info.Feature["SensorId"] is int id)) return;
            var s = _currentSensors.FirstOrDefault(x => x.SensorId == id);
            if (s == null) return;

            var last = await _measurementRepo.GetLatestForSensorAsync(id);
            var reason = GetWarningReason(s, last);

            // sensor.DisplayName if set, else fallback
            var title = !string.IsNullOrEmpty(s.DisplayName)
                ? s.DisplayName
                : $"Sensor {s.SensorId}";

            // value + unit
            var unit = s.Measurand.Unit;
            var valueStr = last?.Value.HasValue == true
                ? $"{last.Value.Value} {unit}"
                : "N/A";

            // age string
            var ageStr = last?.Timestamp.HasValue == true
                ? FormatTimeSpan(DateTime.UtcNow - last.Timestamp.Value) + " ago"
                : "N/A";

            // build message
            var msg =
                $"Status: {s.Status}\n" +
                $"Last reading: {valueStr}\n" +
                $"When: {ageStr}\n\n" +
                (reason != null
                    ? $"⚠️ {reason}"
                    : "All readings OK");

            await _dialogService.DisplayAlertAsync(title, msg, "OK");
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
