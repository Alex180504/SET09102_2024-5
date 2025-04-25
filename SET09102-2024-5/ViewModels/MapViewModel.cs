using System;
using System.Linq;
using System.Threading.Tasks;
using Mapsui;                // for MPoint
using Mapsui.Layers;         // for MemoryLayer, IFeature
using Mapsui.Providers;      // for MemoryProvider
using Mapsui.Styles;         // for SymbolStyle
using Mapsui.UI;             // for BitmapRegistry
using SET09102_2024_5.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.Logging;  // for MainThread

namespace SET09102_2024_5.ViewModels
{
    public class MapViewModel
    {
        public MemoryLayer SensorLayer { get; }

        readonly SensorService _sensorService;
        readonly int _defaultPin, _warningPin, _criticalPin;
        private readonly ILogger<MapViewModel> _logger;

        public MapViewModel(SensorService sensorService, ILogger<MapViewModel> logger)
        {
            _sensorService = sensorService;
            _logger = logger;

            // register pin images once (returns int IDs) :contentReference[oaicite:4]{index=4}
            var asm = typeof(MapViewModel).Assembly;
            _defaultPin = BitmapRegistry.Instance.Register(asm, "SET09102_2024_5.Resources.Images.pin_default.svg");
            _warningPin = BitmapRegistry.Instance.Register(asm, "SET09102_2024_5.Resources.Images.pin_warning.svg");
            _criticalPin = BitmapRegistry.Instance.Register(asm, "SET09102_2024_5.Resources.Images.pin_critical.svg");

            SensorLayer = new MemoryLayer
            {
                Name = "Sensors",
                Features = Enumerable.Empty<IFeature>(),
                Style = new SymbolStyle
                {
                    BitmapId = _defaultPin,
                    SymbolScale = 0.5
                }
            };

            // refresh whenever sensors update
            _sensorService.OnSensorUpdated += (_, __) =>
                MainThread.BeginInvokeOnMainThread(async () => await RefreshAsync());

            _ = Task.Run(async () =>
            {
                await RefreshAsync();
                await _sensorService.StartAsync(TimeSpan.FromSeconds(5));
            });
            _ = DumpAllSensorsAsync();
        }
        private async Task DumpAllSensorsAsync()
        {
            try
            {
                var all = await _sensorService.GetAllWithConfigurationAsync();
                _logger.LogInformation("=== SENSOR DUMP BEGIN ===");
                foreach (var s in all)
                {
                    _logger.LogInformation(
                        "Sensor {Id}: Type={Type}, Status={Status}, " +
                        "Lat={Lat}, Lon={Lon}",
                        s.SensorId,
                        s.SensorType,
                        s.Status,
                        s.Configuration?.Latitude,
                        s.Configuration?.Longitude
                    );
                }
                _logger.LogInformation("=== SENSOR DUMP END ({Count} sensors) ===", all.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read sensor data");
            }
        }

        private async Task RefreshAsync()
        {
            var sensors = await _sensorService.GetAllWithConfigurationAsync();

            var features = sensors.Select(s =>
            {
                // cast float? → double
                var lon = s.Configuration.Longitude ?? 0.0;
                var lat = s.Configuration.Latitude ?? 0.0;

                var pf = new PointFeature(new MPoint(lon, lat))
                {
                    ["Status"] = s.Status
                };

                var bmp = s.Status switch
                {
                    "Critical" => _criticalPin,
                    "Warning" => _warningPin,
                    _ => _defaultPin
                };

                pf.Styles.Add(new SymbolStyle
                {
                    BitmapId = bmp,
                    SymbolScale = 0.5
                });

                return pf as IFeature;
            })
            .ToList();

            SensorLayer.Features = features;  // replaces DataSource in v5 :contentReference[oaicite:5]{index=5}
        }
    }
}
