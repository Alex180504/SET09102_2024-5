using System;
using System.Linq;
using System.Threading.Tasks;
using Mapsui;                // for MPoint
using Mapsui.Layers;         // for MemoryLayer, IFeature
using Mapsui.Providers;      // for MemoryProvider
using Mapsui.Styles;         // for SymbolStyle
using Mapsui.UI;             // for BitmapRegistry
using SET09102_2024_5.Services;
using Microsoft.Maui.ApplicationModel;  // for MainThread

namespace SET09102_2024_5.ViewModels
{
    public class MapViewModel
    {
        public MemoryLayer SensorLayer { get; }

        readonly SensorService _sensorService;
        readonly int _defaultPin, _warningPin, _criticalPin;

        public MapViewModel(SensorService sensorService)
        {
            _sensorService = sensorService;

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
