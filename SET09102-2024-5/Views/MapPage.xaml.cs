using Microsoft.Maui.Controls;
using Mapsui.UI.Maui;      // for MapControl
using Mapsui.Tiling;       // for OpenStreetMap
using Mapsui;              // for MPoint
using Mapsui.Layers;       // for MemoryLayer
using Mapsui.Styles;       // for SymbolStyle
using SET09102_2024_5.ViewModels;
using Mapsui.UI;

namespace SET09102_2024_5.Views
{
    public partial class MapPage : ContentPage
    {
        public MapPage(MapViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;

            var mapControl = new Mapsui.UI.Maui.MapControl();

            // 1) Add free OpenStreetMap tiles
            mapControl.Map?.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
            Content = mapControl;

            // 2) Add your live sensor layer
            mapControl.Map?.Layers.Add(vm.SensorLayer);

            // 3) Zoom to the extent of your sensors, or show world if empty
            var extent = vm.SensorLayer.Extent; // MRect?
            if (extent != null)
            {
                // ZoomToBox replaces old NavigateTo :contentReference[oaicite:1]{index=1}
                mapControl.Map?.Navigator.ZoomToBox(extent, MBoxFit.Fit);
            }
            else
            {
                mapControl.Map?.Navigator.ZoomToBox(
                  new MRect(-180, -90, 180, 90), // world
                  MBoxFit.Fit);
            }
        }
    }
}
