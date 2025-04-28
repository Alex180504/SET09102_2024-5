using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SET09102_2024_5.ViewModels
{
    /// <summary>
    /// ViewModel for the main page of the application
    /// </summary>
    /// <remarks>
    /// This ViewModel handles the main page functionality including a simple counter demonstration.
    /// </remarks>
    public partial class MainPageViewModel : BaseViewModel
    {
        /// <summary>
        /// Gets or sets the counter value that tracks button clicks
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CounterText))]
        private int _count;

        /// <summary>
        /// Gets a formatted string representation of the current count value
        /// </summary>
        public string CounterText => Count == 1 ? $"Clicked {Count} time" : $"Clicked {Count} times";

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPageViewModel"/> class
        /// </summary>
        public MainPageViewModel()
        {
            Title = "Main Page";
        }

        /// <summary>
        /// Increments the counter value when the button is clicked
        /// </summary>
        [RelayCommand]
        private void IncrementCount()
        {
            Count++;
        }

        /// <summary>
        /// Navigates to the sensor management page
        /// </summary>
        private async void OnNavigateToSensorManagement()
        {
            await Shell.Current.GoToAsync("SensorManagementPage");
        }
    }
}
