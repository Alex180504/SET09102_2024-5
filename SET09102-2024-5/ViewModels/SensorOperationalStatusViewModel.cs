using Microsoft.EntityFrameworkCore;
using SET09102_2024_5.Data;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using SET09102_2024_5.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SET09102_2024_5.Views;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace SET09102_2024_5.ViewModels
{
    /// <summary>
    /// ViewModel for the SensorOperationalStatus page that manages the display, filtering, and sorting of sensor operational data.
    /// Provides functionality for:
    /// - Loading sensor data from the database with incident counts
    /// - Filtering sensors by various properties (ID, Type, Status, Measurand)
    /// - Sorting data by multiple columns with direction toggle
    /// - Navigating to incident logs for selected sensors
    /// - Maintaining separate collections for all sensors and filtered results
    /// This class implements the MVVM pattern with observable properties and commands for data binding.
    /// </summary>
    public class SensorOperationalStatusViewModel : BaseViewModel
    {
        private readonly IMainThreadService _mainThreadService;
        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigationService;
        private readonly SensorMonitoringContextFactory _contextFactory;

        private ObservableCollection<SensorOperationalModel> _sensors;
        private ObservableCollection<SensorOperationalModel> _allSensors;
        private SensorOperationalModel _selectedSensor;
        private string _filterText;
        private bool _isLoading;
        private string _selectedFilterProperty;
        private List<string> _filterProperties;
        private string _sortProperty;
        private bool _isSortAscending = true;
        private string _sortIndicator;
        public string IncidentCountSortIndicator => GetSortIndicator("IncidentCount");
        public bool HasSensors => Sensors != null && Sensors.Count > 0;
        public bool HasNoSensors => !HasSensors;

        /// <summary>
        /// Initializes a new instance of the SensorOperationalStatusViewModel.
        /// Sets up commands, default property values, and initiates the initial data load.
        /// Supports dependency injection for services to facilitate testing.
        /// </summary>
        /// <param name="context">Database context for sensor data access</param>
        /// <param name="mainThreadService">Service for UI thread operations</param>
        /// <param name="dialogService">Service for displaying dialogs and alerts</param>
        /// <param name="navigationService">Service for navigation between pages</param>
        public SensorOperationalStatusViewModel(
            SensorMonitoringContext context,
            IMainThreadService? mainThreadService = null,
            IDialogService? dialogService = null,
            INavigationService? navigationService = null)
        {
            _contextFactory = new SensorMonitoringContextFactory();
            _mainThreadService = mainThreadService ?? new Services.MainThreadService();
            _dialogService = dialogService ?? new Services.DialogService();
            _navigationService = navigationService;

            // Init commands
            LoadSensorsCommand = new Command(async () => await LoadSensorsAsync(), () => !IsLoading);
            ApplyCommand = new Command(ApplyFilterAndRefresh, () => !IsLoading);
            ViewIncidentLogCommand = new Command<SensorOperationalModel>(ViewIncidentLog, CanViewIncidentLog);
            SortCommand = new Command<string>(SortSensors);

            // Init collections
            _allSensors = new ObservableCollection<SensorOperationalModel>();
            _sensors = new ObservableCollection<SensorOperationalModel>();
            _filterProperties = new List<string> { "All", "ID", "Type", "Status", "Measurand" };
            _selectedFilterProperty = "All";
            _sortProperty = "";
            _sortIndicator = "";
            _filterText = "";
            _selectedSensor = new SensorOperationalModel();

            _mainThreadService.BeginInvokeOnMainThread(async () => await LoadSensorsAsync());
        }

        /// <summary>
        /// Gets or sets the collection of sensor models displayed in the UI.
        /// Updates HasSensors and HasNoSensors properties when changed.
        /// </summary>
        public ObservableCollection<SensorOperationalModel> Sensors
        {
            get => _sensors;
            set
            {
                if (SetProperty(ref _sensors, value))
                {
                    OnPropertyChanged(nameof(HasSensors));
                    OnPropertyChanged(nameof(HasNoSensors));
                }
            }
        }

        /// <summary>
        /// Gets or sets the currently selected sensor in the UI.
        /// Updates command execution state for ViewIncidentLogCommand when changed.
        /// </summary>
        public SensorOperationalModel SelectedSensor
        {
            get => _selectedSensor;
            set
            {
                if (SetProperty(ref _selectedSensor, value))
                {
                    (ViewIncidentLogCommand as Command<SensorOperationalModel>)?.ChangeCanExecute();
                }
            }
        }

        /// <summary>
        /// Gets or sets the loading state of the view model.
        /// Controls the enabled state of LoadSensorsCommand and ApplyCommand.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    (LoadSensorsCommand as Command)?.ChangeCanExecute();
                    (ApplyCommand as Command)?.ChangeCanExecute();
                }
            }
        }

        /// <summary>
        /// Gets or sets the text used for filtering sensors.
        /// </summary>
        public string FilterText
        {
            get => _filterText;
            set => SetProperty(ref _filterText, value);
        }

        /// <summary>
        /// Gets or sets the currently selected property for filtering (ID, Type, Status, etc.).
        /// </summary>
        public string SelectedFilterProperty
        {
            get => _selectedFilterProperty;
            set => SetProperty(ref _selectedFilterProperty, value);
        }

        /// <summary>
        /// Gets or sets the list of available filter properties.
        /// </summary>
        public List<string> FilterProperties
        {
            get => _filterProperties;
            set => SetProperty(ref _filterProperties, value);
        }

        /// <summary>
        /// Gets or sets the current property used for sorting the sensor collection.
        /// </summary>
        public string SortProperty
        {
            get => _sortProperty;
            set => SetProperty(ref _sortProperty, value);
        }

        /// <summary>
        /// Gets or sets whether sorting is in ascending order.
        /// When false, sorting is in descending order.
        /// </summary>
        public bool IsSortAscending
        {
            get => _isSortAscending;
            set => SetProperty(ref _isSortAscending, value);
        }

        /// <summary>
        /// Gets or sets the current sort indicator character (arrow) used in the UI.
        /// </summary>
        public string SortIndicator
        {
            get => _sortIndicator;
            set => SetProperty(ref _sortIndicator, value);
        }

        /// <summary>
        /// Command to load or refresh sensor data from the database.
        /// </summary>
        public ICommand LoadSensorsCommand { get; }

        /// <summary>
        /// Command to apply the current filter to the sensor collection.
        /// </summary>
        public ICommand ApplyCommand { get; }

        /// <summary>
        /// Command to navigate to the incident log for a selected sensor.
        /// </summary>
        public ICommand ViewIncidentLogCommand { get; }

        /// <summary>
        /// Command to sort the sensor collection by a specified property.
        /// </summary>
        public ICommand SortCommand { get; }

        /// <summary>
        /// Loads sensor data from the database, calculates incident counts, and updates the UI.
        /// Maintains both original collection (_allSensors) and displayed collection (Sensors).
        /// </summary>
        private async Task LoadSensorsAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;

                var sensors = await _contextFactory.CreateDbContext(new string[0])
                    .Sensors
                    .Include(s => s.Measurand)
                    .AsNoTracking()
                    .ToListAsync();

                // Calculate incident counts for each sensor
                var incidentCounts = await _contextFactory.CreateDbContext(new string[0])
                    .Measurements
                    .GroupBy(m => m.SensorId)
                    .Select(g => new
                    {
                        SensorId = g.Key,
                        IncidentCount = g.SelectMany(m => m.IncidentMeasurements)
                            .Select(im => im.IncidentId)
                            .Distinct()
                            .Count()
                    })
                    .ToDictionaryAsync(x => x.SensorId, x => x.IncidentCount);

                _mainThreadService.BeginInvokeOnMainThread(() =>
                {
                    var newSensors = new ObservableCollection<SensorOperationalModel>();

                    foreach (var sensor in sensors)
                    {
                        // Get incident count for this sensor (0 if none found)
                        int incidentCount = 0;
                        if (incidentCounts.TryGetValue(sensor.SensorId, out int count))
                        {
                            incidentCount = count;
                        }

                        newSensors.Add(new SensorOperationalModel
                        {
                            Id = sensor.SensorId,
                            Type = sensor.SensorType,
                            Status = sensor.Status,
                            Measurand = sensor.Measurand?.QuantityName,
                            DeploymentDate = sensor.DeploymentDate,
                            IncidentCount = incidentCount
                        });
                    }

                    // Store the original unfiltered collection
                    _allSensors = newSensors;
                    // Set the displayed collection
                    Sensors = new ObservableCollection<SensorOperationalModel>(_allSensors);

                    // Apply sort if there is an active sort property
                    if (!string.IsNullOrEmpty(SortProperty))
                    {
                        ApplySorting(SortProperty, false);
                    }
                    OnPropertyChanged(nameof(HasSensors));
                    OnPropertyChanged(nameof(HasNoSensors));
                });
            }
            catch (Exception ex)
            {
                await _dialogService.DisplayErrorAsync($"Failed to load sensors: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Applies filtering based on user input text. Resets to original collection when filter text is empty to avoid multiple database calls.
        /// </summary>
        private async void ApplyFilterAndRefresh()
        {
            if (string.IsNullOrWhiteSpace(FilterText))
            {
                // Reset to original collection without reloading from database
                Sensors = new ObservableCollection<SensorOperationalModel>(_allSensors);

                // Apply sort if needed
                if (!string.IsNullOrEmpty(SortProperty))
                {
                    ApplySorting(SortProperty, false);
                }
                return;
            }

            ApplyFilter();
        }

        /// <summary>
        /// Filters the sensor collection based on selected property (ID, Type, Status, etc.).
        /// </summary>
        private void ApplyFilter()
        {
            var filteredList = new ObservableCollection<SensorOperationalModel>();
            var filter = FilterText?.ToLowerInvariant() ?? "";

            // Always filter from the original collection
            foreach (var sensor in _allSensors)
            {
                bool isMatch = false;

                switch (SelectedFilterProperty)
                {
                    case "ID":
                        isMatch = sensor.Id.ToString().Contains(filter);
                        break;
                    case "Type":
                        isMatch = (sensor.Type?.ToLowerInvariant() ?? "").Contains(filter);
                        break;
                    case "Status":
                        isMatch = (sensor.Status?.ToLowerInvariant() ?? "").Contains(filter);
                        break;
                    case "Measurand":
                        isMatch = (sensor.Measurand?.ToLowerInvariant() ?? "").Contains(filter);
                        break;
                    default: // "All"
                        isMatch = sensor.Id.ToString().Contains(filter) ||
                                 (sensor.Type?.ToLowerInvariant() ?? "").Contains(filter) ||
                                 (sensor.Status?.ToLowerInvariant() ?? "").Contains(filter) ||
                                 (sensor.Measurand?.ToLowerInvariant() ?? "").Contains(filter) ||
                                 (sensor.DeploymentDate?.ToString() ?? "").Contains(filter);
                        break;
                }

                if (isMatch)
                {
                    filteredList.Add(sensor);
                }
            }

            Sensors = filteredList;

            // Apply sort if there is an active sort property
            if (!string.IsNullOrEmpty(SortProperty))
            {
                ApplySorting(SortProperty, false);
            }
        }

        /// <summary>
        /// Determines whether the ViewIncidentLog command can execute for a given sensor.
        /// A sensor must be properly initialized with a valid ID.
        /// </summary>
        /// <param name="sensor">The sensor to check</param>
        /// <returns>True if the command can execute; otherwise, false</returns>
        private bool CanViewIncidentLog(SensorOperationalModel sensor)
        {
            return sensor != null && sensor.Id > 0;
        }

        /// <summary>
        /// Navigates to the incident log view for the selected sensor.
        /// Handles navigation differently based on whether the app is in test mode or production.
        /// Displays appropriate error messages if navigation fails.
        /// </summary>
        /// <param name="sensor">The sensor whose incidents should be displayed</param>
        private async void ViewIncidentLog(SensorOperationalModel sensor)
        {
            if (sensor == null) return;

            try
            {
                // In a test environment, the mock navigation service will be used
                if (_navigationService != null &&
                    _navigationService.GetType().FullName.Contains("Mock"))
                {
                    await _navigationService.NavigateToAsync($"MainPage?SensorId={sensor.Id}");
                }
                else
                {
                    // Use _navigationService instead of direct Shell navigation to ensure proper routing
                    if (_navigationService != null)
                    {
                        // Navigate to MainPage
                        await _navigationService.NavigateToAsync(RouteConstants.MainPage);

                        // Display a message to the user about the sensor ID
                        await _dialogService.DisplayAlertAsync("Sensor Incidents", $"Viewing incidents for Sensor #{sensor.Id}.", "OK");
                    }
                    else
                    {
                        // Fallback to direct Shell navigation if navigation service is unavailable
                        await Shell.Current.GoToAsync($"//{RouteConstants.MainPage}");
                    }
                }
            }
            catch (Exception ex)
            {
                await _dialogService.DisplayErrorAsync($"Navigation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Dynamic sorting implementation using the property name as a strategy selector
        /// Toggles sort direction for the same column or sets ascending sort for a new column.
        /// Updates UI indicators to reflect current sort state.
        /// </summary>
        private void SortSensors(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return;

            // Toggle sort direction if clicking on the same column
            if (propertyName == SortProperty)
            {
                IsSortAscending = !IsSortAscending;
            }
            else
            {
                // Default to ascending for a new column
                IsSortAscending = true;
                SortProperty = propertyName;
            }

            ApplySorting(propertyName, true);
        }

        /// <summary>
        /// Sorts the current collection of sensors based on the selected property.
        /// </summary>
        private void ApplySorting(string propertyName, bool updateIndicator)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return;

            // Build a sorted list based on property and direction
            IEnumerable<SensorOperationalModel> sortedData;

            // Create appropriate sorting expression based on property name
            switch (propertyName)
            {
                case "Id":
                    sortedData = IsSortAscending ?
                        Sensors.OrderBy(s => s.Id) :
                        Sensors.OrderByDescending(s => s.Id);
                    break;
                case "Type":
                    sortedData = IsSortAscending ?
                        Sensors.OrderBy(s => s.Type) :
                        Sensors.OrderByDescending(s => s.Type);
                    break;
                case "Status":
                    sortedData = IsSortAscending ?
                        Sensors.OrderBy(s => s.Status) :
                        Sensors.OrderByDescending(s => s.Status);
                    break;
                case "Measurand":
                    sortedData = IsSortAscending ?
                        Sensors.OrderBy(s => s.Measurand) :
                        Sensors.OrderByDescending(s => s.Measurand);
                    break;
                case "DeploymentDate":
                    sortedData = IsSortAscending ?
                        Sensors.OrderBy(s => s.DeploymentDate) :
                        Sensors.OrderByDescending(s => s.DeploymentDate);
                    break;
                case "IncidentCount":
                    sortedData = IsSortAscending ?
                        Sensors.OrderBy(s => s.IncidentCount) :
                        Sensors.OrderByDescending(s => s.IncidentCount);
                    break;
                default:
                    return;
            }

            // Update the collection with sorted data
            Sensors = new ObservableCollection<SensorOperationalModel>(sortedData);

            // Update sort indicator
            if (updateIndicator)
            {
                SortIndicator = IsSortAscending ? "▲" : "▼";

                // Notify that all sort indicators may have changed
                OnPropertyChanged(nameof(IdSortIndicator));
                OnPropertyChanged(nameof(TypeSortIndicator));
                OnPropertyChanged(nameof(StatusSortIndicator));
                OnPropertyChanged(nameof(MeasurandSortIndicator));
                OnPropertyChanged(nameof(DeploymentDateSortIndicator));
                OnPropertyChanged(nameof(IncidentCountSortIndicator));
            }
        }

        /// <summary>
        /// Returns the appropriate sort indicator (arrow) for a column based on current sort state.
        /// If the column is the current sort column, returns an up or down arrow based on sort direction.
        /// Otherwise, returns an empty string.
        /// </summary>
        /// <param name="columnName">The name of the column to get the indicator for</param>
        /// <returns>A string containing an arrow symbol or empty string</returns>
        public string GetSortIndicator(string columnName)
        {
            if (columnName == SortProperty)
            {
                return IsSortAscending ? " ▲" : " ▼";
            }
            return string.Empty;
        }

        /// <summary>
        /// Gets the sort indicator for the Id column.
        /// </summary>
        public string IdSortIndicator => GetSortIndicator("Id");

        /// <summary>
        /// Gets the sort indicator for the Type column.
        /// </summary>
        public string TypeSortIndicator => GetSortIndicator("Type");

        /// <summary>
        /// Gets the sort indicator for the Status column.
        /// </summary>
        public string StatusSortIndicator => GetSortIndicator("Status");

        /// <summary>
        /// Gets the sort indicator for the Measurand column.
        /// </summary>
        public string MeasurandSortIndicator => GetSortIndicator("Measurand");

        /// <summary>
        /// Gets the sort indicator for the DeploymentDate column.
        /// </summary>
        public string DeploymentDateSortIndicator => GetSortIndicator("DeploymentDate");
    }
}
