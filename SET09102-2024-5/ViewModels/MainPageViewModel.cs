using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Services;
using System.Threading.Tasks;

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
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

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
        public MainPageViewModel(INavigationService navigationService, IDialogService dialogService)
        {
            Title = "Main Page";
            _navigationService = navigationService;
            _dialogService = dialogService;
        }

        /// <summary>
        /// Increments the counter value when the button is clicked
        /// </summary>
        [RelayCommand]
        private void IncrementCount()
        {
            Count++;
        }

        // Navigation Commands

        /// <summary>
        /// Navigates to the sensor management page
        /// </summary>
        [RelayCommand]
        private async Task NavigateToSensorManagement()
        {
            await _navigationService.NavigateToAsync(RouteConstants.SensorManagementPage);
        }

        /// <summary>
        /// Navigates to the sensor locator page
        /// </summary>
        [RelayCommand]
        private async Task NavigateToSensorLocator()
        {
            await _navigationService.NavigateToAsync(RouteConstants.SensorLocatorPage);
        }

        /// <summary>
        /// Navigates to the sensor map page
        /// </summary>
        [RelayCommand]
        private async Task NavigateToSensorMap()
        {
            await _navigationService.NavigateToAsync(RouteConstants.MapPage);
        }

        /// <summary>
        /// Navigates to the sensor monitoring/operational status page
        /// </summary>
        [RelayCommand]
        private async Task NavigateToSensorMonitoring()
        {
            await _navigationService.NavigateToAsync(RouteConstants.SensorOperationalStatusPage);
        }

        /// <summary>
        /// Navigates to the historical data page
        /// </summary>
        [RelayCommand]
        private async Task NavigateToHistoricalData()
        {
            await _navigationService.NavigateToAsync(RouteConstants.HistoricalDataPage);
        }

        /// <summary>
        /// Navigates to the data storage page
        /// </summary>
        [RelayCommand]
        private async Task NavigateToDataStorage()
        {
            await _navigationService.NavigateToAsync(RouteConstants.DataStoragePage);
        }
    }
}
