using System.Windows.Input;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Graphics;
using SET09102_2024_5.Data.Repositories;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using Map = Mapsui.Map;

namespace SET09102_2024_5.ViewModels
{
    public class MapViewModel : BaseViewModel, IDisposable
    {

        public ICommand RefreshCommand { get; }
        public bool HasError { get; private set; }
        public string ErrorMessage { get; private set; }

        public Map Map { get; }

        private readonly CancellationTokenSource _pollingCts = new();
        private readonly MemoryLayer _pinLayer;
        private readonly ISensorService _sensorService;
        private readonly IMainThreadService _mainThread;
        private readonly IMeasurementRepository _measurementRepo;
        private readonly IDialogService _dialogService;
        private readonly ILogger<MapViewModel> _logger;

        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private readonly List<Stream> _pinStreams = new();
        private readonly Dictionary<string, SymbolStyle> _statusStyles = new();
        private List<Sensor> _currentSensors = new();

        public MapViewModel(
            ISensorService sensorService,
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

            // wire up error handling
            _sensorService.OnError += ex =>
            {
                HasError = true;
                ErrorMessage = $"Polling error: {ex.Message}";
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(ErrorMessage));
            };

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

            RefreshCommand = new Command(async () => await SafeRefreshAsync());
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
            await SafeRefreshAsync();

            // start cancellable polling
            _ = _sensorService.StartAsync(TimeSpan.FromSeconds(5), _pollingCts.Token);
        }

        private async Task SafeRefreshAsync()
        {
            try
            {
                HasError = false;
                OnPropertyChanged(nameof(HasError));
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Refresh failed: {ex.Message}";
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }

        private void OnSensorUpdated(Sensor _, DateTime? __) =>
            _mainThread.BeginInvokeOnMainThread(async () => await RefreshAsync());

        // Rebuilds the pin layer, choosing a warning style when needed.
        private async Task RefreshAsync()
        {
            if (!await _refreshLock.WaitAsync(0)) return;      // skip if already refreshing

            try
            {
                // Load sensors with their configurations
                var sensors = await _sensorService.GetAllWithConfigurationAsync();
                _currentSensors = sensors;

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
                Map.Refresh();
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

        // Returns a human-readable reason if the sensor is stale/out-of-threshold.
        string? GetWarningReason(Sensor s, MeasurementDto? last)
        {
            // **NEW**: never warn for inactive or maintenance sensors
            if (s.Status == "Inactive" || s.Status == "Maintenance")
                return null;

            if (last == null)
                return "no recent data";

            // Staleness: age > configured frequency
            var freq = s.Configuration?.MeasurementFrequency ?? 0;
            if (freq > 0 && last.Timestamp.HasValue)
            {
                var age = DateTime.UtcNow - last.Timestamp.Value;
                var threshold = TimeSpan.FromMinutes(freq);
                if (age > threshold)
                    return $"reading is late by {FormatTimeSpan(age - threshold)}";
            }

            // Threshold breach
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
                catch (DirectoryNotFoundException) { }
            }
            
            // Image file not found, create a fallback image programmatically
            _logger.LogWarning("Pin image file '{filename}' not found, creating fallback image", filename);
            
            try 
            {
                return CreateFallbackPinImage(filename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create fallback pin image for '{filename}'", filename);
                return -1;
            }
        }
        
        private int CreateFallbackPinImage(string filename)
        {
            // Determine color based on filename
            byte r, g, b;
            if (filename.Contains("warning"))
            {
                // Red
                r = 255; g = 0; b = 0;
            }
            else if (filename.Contains("neutral"))
            {
                // Gray
                r = 128; g = 128; b = 128;
            }
            else
            {
                // Green
                r = 0; g = 255; b = 0;
            }
                
            // Create a simple circular pin image 
            const int size = 64;
            
            using var stream = new MemoryStream();
            CreateSimplePng(stream, size, r, g, b);
            
            // Reset stream position
            stream.Position = 0;
            
            // Register with BitmapRegistry
            var id = BitmapRegistry.Instance.Register(stream);
            if (id > 0) _pinStreams.Add(stream);
            return id;
        }
        
        private void CreateSimplePng(Stream stream, int size, byte r, byte g, byte b)
        {
            // This is a very basic PNG creation - it will create a colored circle
            
            // PNG signature
            byte[] signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            stream.Write(signature, 0, signature.Length);
            
            // IHDR chunk
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, true))
            {
                // IHDR length
                writer.Write(SwapEndian(13));
                
                // IHDR chunk type
                writer.Write(new byte[] { 0x49, 0x48, 0x44, 0x52 });
                
                // Width and height
                writer.Write(SwapEndian(size));
                writer.Write(SwapEndian(size));
                
                // Bit depth (8), Color type (6 = RGBA), other settings
                writer.Write(new byte[] { 8, 6, 0, 0, 0 });
                
                // CRC (placeholder as we're simplifying)
                writer.Write(new byte[] { 0, 0, 0, 0 });
                
                // IDAT chunk
                int dataSize = size * size * 4 + size; // RGBA + filter byte per row
                writer.Write(SwapEndian(dataSize));
                writer.Write(new byte[] { 0x49, 0x44, 0x41, 0x54 });
                
                for (int y = 0; y < size; y++)
                {
                    // Filter type (0 = None)
                    writer.Write((byte)0);
                    
                    for (int x = 0; x < size; x++)
                    {
                        // Determine alpha based on distance from center (for circle)
                        int dx = x - size/2;
                        int dy = y - size/2;
                        double distance = Math.Sqrt(dx*dx + dy*dy);
                        byte alpha = distance <= (size/2 - 2) ? (byte)255 : (byte)0;
                        
                        // RGBA pixels
                        writer.Write(r);
                        writer.Write(g);
                        writer.Write(b);
                        writer.Write(alpha);
                    }
                }
                
                // CRC (placeholder)
                writer.Write(new byte[] { 0, 0, 0, 0 });
                
                // IEND chunk
                writer.Write(SwapEndian(0));
                writer.Write(new byte[] { 0x49, 0x45, 0x4E, 0x44 });
                writer.Write(new byte[] { 0xAE, 0x42, 0x60, 0x82 }); // CRC for IEND
            }
        }
        
        private uint SwapEndian(int value)
        {
            return (uint)((value & 0xFF) << 24 | (value & 0xFF00) << 8 | 
                   (value & 0xFF0000) >> 8 | (value & 0xFF000000) >> 24);
        }

        public void Stop()
        {
            // tell the polling loop to end:
            _pollingCts.Cancel();
            _sensorService.OnSensorUpdated -= OnSensorUpdated;
        }

        public void Dispose()
        {
            _pollingCts.Cancel();
            _pollingCts.Dispose();
            Stop();
            Map.Info -= OnMapInfo;
            _refreshLock.Dispose();
            foreach (var ms in _pinStreams) ms.Dispose();
            _pinStreams.Clear();
        }
    }
}
