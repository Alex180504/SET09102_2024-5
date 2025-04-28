using SET09102_2024_5.ViewModels;
using SET09102_2024_5.Models;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace SET09102_2024_5.Views
{
    public partial class SensorManagementPage : ViewBase
    {
        private SensorManagementViewModel ViewModel => BindingContext as SensorManagementViewModel;

        // Add parameterless constructor for Shell navigation
        public SensorManagementPage()
        {
            InitializeComponent();
            BindingContext = App.Current.Handler.MauiContext?.Services.GetService<SensorManagementViewModel>();
        }

        public SensorManagementPage(SensorManagementViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (ViewModel == null) return;

            var searchText = e.NewTextValue;
            ViewModel.FilterSensors(searchText);
        }

        private void OnSensorSelected(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null || e.CurrentSelection.Count == 0) return;

            var selectedSensor = e.CurrentSelection[0] as Models.Sensor;
            ViewModel.SelectedSensor = selectedSensor;

            // Clear selection to allow reselection of the same item
            filteredSensorsView.SelectedItem = null;
        }

        private void OnSearchButtonPressed(object sender, EventArgs e)
        {
            if (ViewModel == null) return;

            // Hide the keyboard
            var searchBar = sender as SearchBar;
            searchBar?.Unfocus();

            // Always collapse search results when search button is pressed
            ViewModel.HideSearchResults();
        }

        private void OnSearchBarFocused(object sender, FocusEventArgs e)
        {
            if (ViewModel == null) return;

            if (e.IsFocused)
            {
                // When search bar gets focus, show all sensors in the dropdown
                ViewModel.ShowAllSensorsInSearch();
            }
        }

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

        private void OnOrientationChanged(object sender, EventArgs e)
        {
            if (ViewModel == null) return;

            ViewModel.ValidateCommand.Execute(ConfigurationConstants.Orientation);
        }
    }
}
