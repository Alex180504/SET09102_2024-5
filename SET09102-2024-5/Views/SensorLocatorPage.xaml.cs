using Mapsui.UI.Maui;
using SET09102_2024_5.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace SET09102_2024_5.Views
{
    /// <summary>
    /// Page that provides map-based sensor location visualization and search functionality.
    /// Allows users to locate sensors on a map, search for specific sensors, and plan routes between sensors.
    /// Integrates with the SensorLocatorViewModel for data management and map operations.
    /// </summary>
    public partial class SensorLocatorPage : ViewBase
    {
        /// <summary>
        /// Gets the SensorLocatorViewModel from the current BindingContext.
        /// Provides a convenient shorthand for accessing the view model's properties and methods.
        /// </summary>
        private SensorLocatorViewModel _viewModel => BindingContext as SensorLocatorViewModel;

        /// <summary>
        /// Initializes a new instance of the SensorLocatorPage class with dependencies resolved via DI.
        /// This parameterless constructor is required for Shell navigation.
        /// </summary>
        public SensorLocatorPage()
        {
            InitializeComponent();
            BindingContext = App.Current.Handler.MauiContext?.Services.GetService<SensorLocatorViewModel>();
            if (BindingContext is SensorLocatorViewModel vm)
            {
                MapControl.Map = vm.Map;
            }
        }

        /// <summary>
        /// Initializes a new instance of the SensorLocatorPage class with an explicit view model.
        /// This constructor is primarily used for testing or when manual dependency injection is needed.
        /// </summary>
        /// <param name="viewModel">The SensorLocatorViewModel instance to use as the BindingContext</param>
        public SensorLocatorPage(SensorLocatorViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;

            // Hook up the VM's Map instance
            MapControl.Map = viewModel.Map;
        }

        /// <summary>
        /// Called when the page is about to become visible.
        /// Initializes the view model to load sensor data, setup map features, and get user location.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Initialize the viewmodel when the page appears
            await _viewModel.InitializeAsync();
        }

        /// <summary>
        /// Handles the TextChanged event from the search bar.
        /// Filters the list of sensors based on the entered search text.
        /// </summary>
        /// <param name="sender">The search bar that triggered the event</param>
        /// <param name="e">Event arguments containing the old and new text values</param>
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_viewModel == null) return;

            var searchText = e.NewTextValue;
            _viewModel.FilterSensors(searchText);
        }

        /// <summary>
        /// Handles the SelectionChanged event from the sensor list view.
        /// Updates the selected sensor in the view model and clears the selection to allow reselection.
        /// </summary>
        /// <param name="sender">The list view that triggered the event</param>
        /// <param name="e">Event arguments containing the selected items</param>
        private void OnSensorSelected(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel == null || e.CurrentSelection.Count == 0) return;

            var selectedSensor = e.CurrentSelection[0] as Models.Sensor;
            _viewModel.SelectedSensor = selectedSensor;

            // Clear selection to allow reselection of the same item
            sensorListView.SelectedItem = null;
        }

        /// <summary>
        /// Handles the SearchButtonPressed event from the search bar.
        /// Hides the keyboard and collapses the search results panel.
        /// </summary>
        /// <param name="sender">The search bar that triggered the event</param>
        /// <param name="e">Event arguments</param>
        private void OnSearchButtonPressed(object sender, EventArgs e)
        {
            if (_viewModel == null) return;

            // Hide the keyboard
            var searchBar = sender as SearchBar;
            searchBar?.Unfocus();

            // Always collapse search results when search button is pressed
            _viewModel.HideSearchResults();
        }

        /// <summary>
        /// Handles the Focused event from the search bar.
        /// Shows the search results panel and filters sensors when the search bar receives focus.
        /// </summary>
        /// <param name="sender">The search bar that triggered the event</param>
        /// <param name="e">Event arguments containing the focus state</param>
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

        /// <summary>
        /// Handles the Unfocused event from the search bar.
        /// Hides the search results panel after a short delay when the search bar loses focus.
        /// Uses the main thread to update UI elements safely.
        /// </summary>
        /// <param name="sender">The search bar that triggered the event</param>
        /// <param name="e">Event arguments containing the focus state</param>
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

        /// <summary>
        /// Handles the SelectedIndexChanged event from the travel mode picker.
        /// Notifies the view model about the change in travel mode for route calculations.
        /// </summary>
        /// <param name="sender">The picker that triggered the event</param>
        /// <param name="e">Event arguments</param>
        private void OnTravelModePickerSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_viewModel == null) return;
            // Note: This method appears to be incomplete in the implementation
            // Typically, it would update the view model's selected travel mode
        }
    }
}
