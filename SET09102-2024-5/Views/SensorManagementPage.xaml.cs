using SET09102_2024_5.ViewModels;
using SET09102_2024_5.Models;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace SET09102_2024_5.Views
{
    /// <summary>
    /// Page that provides the user interface for sensor configuration management.
    /// Allows users to search for sensors, view their configurations, and edit settings
    /// such as location coordinates, thresholds, and operational parameters.
    /// Implements field validation and integrates with the SensorManagementViewModel.
    /// </summary>
    public partial class SensorManagementPage : ViewBase
    {
        /// <summary>
        /// Gets the SensorManagementViewModel from the current BindingContext.
        /// Provides a convenient shorthand for accessing the view model.
        /// </summary>
        private SensorManagementViewModel ViewModel => BindingContext as SensorManagementViewModel;

        /// <summary>
        /// Initializes a new instance of the SensorManagementPage class with dependencies resolved via DI.
        /// This parameterless constructor is required for Shell navigation.
        /// </summary>
        public SensorManagementPage()
        {
            InitializeComponent();
            BindingContext = App.Current.Handler.MauiContext?.Services.GetService<SensorManagementViewModel>();
        }

        /// <summary>
        /// Initializes a new instance of the SensorManagementPage class with an explicit view model.
        /// This constructor is primarily used for unit testing or when manually injecting dependencies.
        /// </summary>
        /// <param name="viewModel">The SensorManagementViewModel instance to use</param>
        public SensorManagementPage(SensorManagementViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        /// <summary>
        /// Handles the TextChanged event for the search bar.
        /// Filters the sensor list in real-time as the user types.
        /// </summary>
        /// <param name="sender">The control that triggered the event</param>
        /// <param name="e">Event arguments containing the current search text</param>
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (ViewModel == null) return;

            var searchText = e.NewTextValue;
            ViewModel.FilterSensors(searchText);
        }

        /// <summary>
        /// Handles the SelectionChanged event for the sensor list.
        /// Updates the selected sensor in the view model and loads its configuration details.
        /// Clears the selection to allow reselection of the same sensor.
        /// </summary>
        /// <param name="sender">The list view that triggered the event</param>
        /// <param name="e">Event arguments containing the selected items</param>
        private void OnSensorSelected(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null || e.CurrentSelection.Count == 0) return;

            var selectedSensor = e.CurrentSelection[0] as Models.Sensor;
            ViewModel.SelectedSensor = selectedSensor;

            // Clear selection to allow reselection of the same item
            filteredSensorsView.SelectedItem = null;
        }

        /// <summary>
        /// Handles the SearchButtonPressed event for the search bar.
        /// Dismisses the keyboard and hides the search results panel.
        /// </summary>
        /// <param name="sender">The search bar that triggered the event</param>
        /// <param name="e">Empty event arguments</param>
        private void OnSearchButtonPressed(object sender, EventArgs e)
        {
            if (ViewModel == null) return;

            // Hide the keyboard
            var searchBar = sender as SearchBar;
            searchBar?.Unfocus();

            // Always collapse search results when search button is pressed
            ViewModel.HideSearchResults();
        }

        /// <summary>
        /// Handles the Focused event for the search bar.
        /// Shows all available sensors in the dropdown when the search bar receives focus.
        /// </summary>
        /// <param name="sender">The search bar that triggered the event</param>
        /// <param name="e">Event arguments containing the focus state</param>
        private void OnSearchBarFocused(object sender, FocusEventArgs e)
        {
            if (ViewModel == null) return;

            if (e.IsFocused)
            {
                // When search bar gets focus, show all sensors in the dropdown
                ViewModel.ShowAllSensorsInSearch();
            }
        }

        /// <summary>
        /// Handles the Unfocused event for the search bar.
        /// Hides search results after a short delay, but only if the search text is empty
        /// or if a sensor has been selected, to prevent disrupting the user experience.
        /// </summary>
        /// <param name="sender">The search bar that triggered the event</param>
        /// <param name="e">Event arguments containing the focus state</param>
        private void OnSearchBarUnfocused(object sender, FocusEventArgs e)
        {
            if (ViewModel == null) return;

            Task.Delay(200).ContinueWith(_ =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Only hide if no text in search bar or if a selection has been made
                    if (string.IsNullOrWhiteSpace(ViewModel.SearchText) || ViewModel.SelectedSensor != null)
                    {
                        ViewModel.HideSearchResults();
                    }
                });
            });
        }

        /// <summary>
        /// Handles the Unfocused event for configuration field entries.
        /// Triggers validation for the specific field when the user completes editing.
        /// Identifies the field using the ClassId property of the entry.
        /// </summary>
        /// <param name="sender">The entry control that triggered the event</param>
        /// <param name="e">Event arguments containing the focus state</param>
        private void OnFieldUnfocused(object sender, FocusEventArgs e)
        {
            if (ViewModel == null) return;

            if (sender is Entry entry && !string.IsNullOrEmpty(entry.ClassId))
            {
                string fieldName = entry.ClassId;

                if (fieldName == "Orientation")
                {
                    fieldName = nameof(Configuration.Orientation);
                }

                ViewModel.ValidateCommand.Execute(fieldName);
            }
        }

        /// <summary>
        /// Handles the orientation picker's selection changed event.
        /// Triggers validation for the orientation field when the user selects a new value.
        /// </summary>
        /// <param name="sender">The picker control that triggered the event</param>
        /// <param name="e">Empty event arguments</param>
        private void OnOrientationChanged(object sender, EventArgs e)
        {
            if (ViewModel == null) return;

            ViewModel.ValidateCommand.Execute(ConfigurationConstants.Orientation);
        }
    }
}
