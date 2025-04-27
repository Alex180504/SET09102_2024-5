using Mapsui.UI.Maui;
using SET09102_2024_5.ViewModels;

namespace SET09102_2024_5.Views
{
    public partial class MapPage : ContentPage
    {
        private readonly MapViewModel _vm;

        public MapPage(MapViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;

            // Hook up the VM's Map instance
            MapControl.Map = _vm.Map;
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
