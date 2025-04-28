using Mapsui.UI.Maui;
using SET09102_2024_5.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace SET09102_2024_5.Views
{
    public partial class MapPage : ViewBase
    {
        private MapViewModel _vm => BindingContext as MapViewModel;

        // Add parameterless constructor for Shell navigation
        public MapPage()
        {
            InitializeComponent();
            BindingContext = App.Current.Handler.MauiContext?.Services.GetService<MapViewModel>();
            if (BindingContext is MapViewModel vm)
            {
                MapControl.Map = vm.Map;
            }
        }

        public MapPage(MapViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;

            // Hook up the VM's Map instance
            MapControl.Map = vm.Map;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Kick off loading + polling
            await _vm.InitializeAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Stop polling and unsubscribe
            _vm.Stop();
        }
    }
}
