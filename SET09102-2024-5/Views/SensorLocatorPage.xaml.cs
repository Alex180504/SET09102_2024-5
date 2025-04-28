using Mapsui.UI.Maui;
using SET09102_2024_5.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace SET09102_2024_5.Views
{
    public partial class SensorLocatorPage : ViewBase
    {
        private SensorLocatorViewModel _viewModel => BindingContext as SensorLocatorViewModel;

        // Add parameterless constructor for Shell navigation
        public SensorLocatorPage()
        {
            InitializeComponent();
            BindingContext = App.Current.Handler.MauiContext?.Services.GetService<SensorLocatorViewModel>();
            if (BindingContext is SensorLocatorViewModel vm)
            {
                MapControl.Map = vm.Map;
            }
        }

        public SensorLocatorPage(SensorLocatorViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;

            // Hook up the VM's Map instance
            MapControl.Map = viewModel.Map;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Initialize the viewmodel when the page appears
            await _viewModel.InitializeAsync();
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_viewModel == null) return;

            var searchText = e.NewTextValue;
            _viewModel.FilterSensors(searchText);
        }

        private void OnSensorSelected(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel == null || e.CurrentSelection.Count == 0) return;

            var selectedSensor = e.CurrentSelection[0] as Models.Sensor;
            _viewModel.SelectedSensor = selectedSensor;

            // Clear selection to allow reselection of the same item
            sensorListView.SelectedItem = null;
        }

        private void OnSearchButtonPressed(object sender, EventArgs e)
        {
            if (_viewModel == null) return;

            // Hide the keyboard
            var searchBar = sender as SearchBar;
            searchBar?.Unfocus();

            // Always collapse search results when search button is pressed
            _viewModel.HideSearchResults();
        }

        private void OnSearchBarFocused(object sender, FocusEventArgs e)
        {
            if (_viewModel == null) return;

            if (e.IsFocused)
            {
                // When search bar gets focus, show sensor list
                searchResultsFrame.IsVisible = true;
                _viewModel.FilterSensors(_viewModel.SearchText);
            }
        }

        private void OnSearchBarUnfocused(object sender, FocusEventArgs e)
        {
            if (_viewModel == null) return;

            Task.Delay(200).ContinueWith(_ =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Only hide if a selection has been made
                    searchResultsFrame.IsVisible = false;
                    _viewModel.HideSearchResults();
                });
            });
        }

        private void OnTravelModePickerSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_viewModel == null) return;
        }
    }
}
