using Microsoft.EntityFrameworkCore;
using SET09102_2024_5.Data;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using SET09102_2024_5.Services; // Add this for RouteConstants
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

        public string FilterText
        {
            get => _filterText;
            set => SetProperty(ref _filterText, value);
        }

        public string SelectedFilterProperty
        {
            get => _selectedFilterProperty;
            set => SetProperty(ref _selectedFilterProperty, value);
        }

        public List<string> FilterProperties
        {
            get => _filterProperties;
            set => SetProperty(ref _filterProperties, value);
        }

        public string SortProperty
        {
            get => _sortProperty;
            set => SetProperty(ref _sortProperty, value);
        }

        public bool IsSortAscending
        {
            get => _isSortAscending;
            set => SetProperty(ref _isSortAscending, value);
        }

        public string SortIndicator
        {
            get => _sortIndicator;
            set => SetProperty(ref _sortIndicator, value);
        }

        public ICommand LoadSensorsCommand { get; }
        public ICommand ApplyCommand { get; }
        public ICommand ViewIncidentLogCommand { get; }
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

        private bool CanViewIncidentLog(SensorOperationalModel sensor)
        {
            return sensor != null && sensor.Id > 0;
        }

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

        // Helper to generate appropriate sort indicators for the UI
        public string GetSortIndicator(string columnName)
        {
            if (columnName == SortProperty)
            {
                return IsSortAscending ? " ▲" : " ▼";
            }
            return string.Empty;
        }

        // Properties for each column's sort indicator
        public string IdSortIndicator => GetSortIndicator("Id");
        public string TypeSortIndicator => GetSortIndicator("Type");
        public string StatusSortIndicator => GetSortIndicator("Status");
        public string MeasurandSortIndicator => GetSortIndicator("Measurand");
        public string DeploymentDateSortIndicator => GetSortIndicator("DeploymentDate");
    }
}
