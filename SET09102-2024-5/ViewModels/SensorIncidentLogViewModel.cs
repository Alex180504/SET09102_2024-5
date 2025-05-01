// SensorIncidentLogViewModel.cs
using Microsoft.EntityFrameworkCore;
using SET09102_2024_5.Data;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SET09102_2024_5.ViewModels
{
    /// <summary>
    /// ViewModel for displaying and managing sensor incident logs.
    /// Provides functionality for loading, filtering, and sorting incident records
    /// associated with a specific sensor.
    /// </summary>
    public class SensorIncidentLogViewModel : BaseViewModel, IQueryAttributable
    {
        private readonly SensorMonitoringContext _context;
        private readonly IMainThreadService _mainThreadService;
        private readonly IDialogService _dialogService;

        private int _sensorId;
        private string _sensorInfo;
        private ObservableCollection<IncidentModel> _incidents;
        private ObservableCollection<IncidentModel> _allIncidents;
        private IncidentModel _selectedIncident;
        private string _filterText;
        private bool _isLoading;
        private string _selectedFilterProperty;
        private List<string> _filterProperties;
        private string _sortProperty;
        private bool _isSortAscending = true;
        private string _sortIndicator;

        /// <summary>
        /// Gets or sets the ID of the sensor whose incidents are being displayed.
        /// This value is typically passed through navigation parameters.
        /// </summary>
        public int SensorId
        {
            get => _sensorId;
            set => SetProperty(ref _sensorId, value);
        }

        /// <summary>
        /// Gets or sets the formatted display information about the current sensor,
        /// including its ID, type, and associated measurand.
        /// </summary>
        public string SensorInfo
        {
            get => _sensorInfo;
            set => SetProperty(ref _sensorInfo, value);
        }

        /// <summary>
        /// Gets or sets the collection of incident models currently displayed in the UI.
        /// This collection may be filtered or sorted based on user interactions.
        /// Setting this property also updates HasIncidents and HasNoIncidents properties.
        /// </summary>
        public ObservableCollection<IncidentModel> Incidents
        {
            get => _incidents;
            set
            {
                if (SetProperty(ref _incidents, value))
                {
                    OnPropertyChanged(nameof(HasIncidents));
                    OnPropertyChanged(nameof(HasNoIncidents));
                }
            }
        }

        /// <summary>
        /// Gets or sets the currently selected incident in the UI.
        /// This can be used for detail views or context actions.
        /// </summary>
        public IncidentModel SelectedIncident
        {
            get => _selectedIncident;
            set => SetProperty(ref _selectedIncident, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether data is currently being loaded.
        /// When true, loading indicators should be displayed and commands are disabled.
        /// Setting this property also updates the command execution states.
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    (LoadIncidentsCommand as Command)?.ChangeCanExecute();
                    (ApplyCommand as Command)?.ChangeCanExecute();
                }
            }
        }

        /// <summary>
        /// Gets or sets the text entered by the user for filtering incidents.
        /// This text is applied to the selected filter property.
        /// </summary>
        public string FilterText
        {
            get => _filterText;
            set => SetProperty(ref _filterText, value);
        }

        /// <summary>
        /// Gets or sets the property name selected for filtering (e.g., "All", "ID", "Priority").
        /// Determines which incident properties are searched when applying filters.
        /// </summary>
        public string SelectedFilterProperty
        {
            get => _selectedFilterProperty;
            set => SetProperty(ref _selectedFilterProperty, value);
        }

        /// <summary>
        /// Gets or sets the list of properties available for filtering incidents.
        /// Typically includes options like "All", "ID", "Priority", "Status", and "Responder".
        /// </summary>
        public List<string> FilterProperties
        {
            get => _filterProperties;
            set => SetProperty(ref _filterProperties, value);
        }

        /// <summary>
        /// Gets or sets the name of the property currently used for sorting.
        /// This corresponds to one of the incident model properties.
        /// </summary>
        public string SortProperty
        {
            get => _sortProperty;
            set => SetProperty(ref _sortProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether sorting is in ascending order.
        /// When false, sorting is in descending order.
        /// </summary>
        public bool IsSortAscending
        {
            get => _isSortAscending;
            set => SetProperty(ref _isSortAscending, value);
        }

        /// <summary>
        /// Gets or sets the visual indicator for sort direction (e.g., "▲" or "▼").
        /// Used to display the current sort direction in the UI.
        /// </summary>
        public string SortIndicator
        {
            get => _sortIndicator;
            set => SetProperty(ref _sortIndicator, value);
        }

        /// <summary>
        /// Gets a value indicating whether there are any incidents to display.
        /// Used for conditional visibility in the UI.
        /// </summary>
        public bool HasIncidents => Incidents != null && Incidents.Count > 0;

        /// <summary>
        /// Gets a value indicating whether there are no incidents to display.
        /// Used for conditional visibility of empty state messaging in the UI.
        /// </summary>
        public bool HasNoIncidents => !HasIncidents;

        /// <summary>
        /// Command to load or reload incidents for the current sensor.
        /// Disabled while loading is in progress.
        /// </summary>
        public ICommand LoadIncidentsCommand { get; }

        /// <summary>
        /// Command to apply the current filter text and selected filter property.
        /// Disabled while loading is in progress.
        /// </summary>
        public ICommand ApplyCommand { get; }

        /// <summary>
        /// Command to sort incidents by a specified property.
        /// Takes a string parameter indicating which property to sort by.
        /// </summary>
        public ICommand SortCommand { get; }

        /// <summary>
        /// Command to navigate back to the previous page.
        /// Uses MAUI Shell navigation.
        /// </summary>
        public ICommand BackCommand { get; }

        public SensorIncidentLogViewModel(
            SensorMonitoringContext context,
            IMainThreadService mainThreadService = null,
            IDialogService dialogService = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mainThreadService = mainThreadService ?? new Services.MainThreadService();
            _dialogService = dialogService ?? new Services.DialogService();

            // Init commands
            LoadIncidentsCommand = new Command(async () => await LoadIncidentsAsync(), () => !IsLoading);
            ApplyCommand = new Command(ApplyFilterAndRefresh, () => !IsLoading);
            SortCommand = new Command<string>(SortIncidents);
            BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));

            // Inite collections
            _allIncidents = new ObservableCollection<IncidentModel>();
            Incidents = new ObservableCollection<IncidentModel>();
            FilterProperties = new List<string> { "All", "ID", "Priority", "Status", "Responder" };
            SelectedFilterProperty = "All";
            SortProperty = "";
            SortIndicator = "";
        }

        /// <summary>
        /// Extracts SensorId from navigation query parameters and initiates data loading.
        /// Implements IQueryAttributable pattern for MAUI Shell navigation.
        /// </summary>
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("SensorId", out var sensorIdObj))
            {
                if (sensorIdObj is string sensorIdString && int.TryParse(sensorIdString, out int sensorId))
                {
                    SensorId = sensorId;
                    _mainThreadService.BeginInvokeOnMainThread(async () => await LoadIncidentsAsync());
                }
            }
        }

        /// <summary>
        /// Fetches sensor and related incident data from the database.
        /// Maps database entities to view models and updates collections for UI display.
        /// Maintains both original collection (_allIncidents) and displayed collection (Incidents).
        /// </summary>
        private async Task LoadIncidentsAsync()
        {
            if (IsLoading || SensorId <= 0) return;

            try
            {
                IsLoading = true;

                var sensor = await _context.Sensors
                    .Include(s => s.Measurand)
                    .FirstOrDefaultAsync(s => s.SensorId == SensorId);

                if (sensor == null)
                {
                    await _dialogService.DisplayErrorAsync($"Sensor with ID {SensorId} not found.");
                    return;
                }

                SensorInfo = $"Sensor {sensor.SensorId} - {sensor.SensorType} ({sensor.Measurand?.QuantityName})";

                // LINQ query to retrieve all incidents linked to the sensor
                var incidents = await _context.Incidents
                    .Where(i => i.IncidentMeasurements.Any(im => im.Measurement.SensorId == SensorId))
                    .Include(i => i.Responder)
                    .AsNoTracking()
                    .ToListAsync();

                _mainThreadService.BeginInvokeOnMainThread(() =>
                {
                    var incidentModels = new ObservableCollection<IncidentModel>(
                        incidents.Select(incident => new IncidentModel
                        {
                            Id = incident.IncidentId,
                            Priority = incident.Priority ?? "Unknown",
                            ResponderName = incident.Responder != null
                                ? $"{incident.Responder.FirstName} {incident.Responder.LastName}"
                                : "Unassigned",
                            ResponderComments = incident.ResponderComments ?? "",
                            Status = incident.ResolvedDate.HasValue ? "Resolved" : "Open",
                            ResolvedDate = incident.ResolvedDate
                        }));

                    // Store the original unfiltered collection
                    _allIncidents = incidentModels;
                    // Set the displayed collection
                    Incidents = new ObservableCollection<IncidentModel>(_allIncidents);

                    // Apply sort if needed
                    if (!string.IsNullOrEmpty(SortProperty))
                    {
                        ApplySorting(SortProperty, false);
                    }
                });
            }
            catch (Exception ex)
            {
                await _dialogService.DisplayErrorAsync($"Failed to load incidents: {ex.Message}");
                _mainThreadService.BeginInvokeOnMainThread(() =>
                {
                    _allIncidents = new ObservableCollection<IncidentModel>();
                    Incidents = new ObservableCollection<IncidentModel>();
                });
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
                Incidents = new ObservableCollection<IncidentModel>(_allIncidents);

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
        /// Filters the incident collection based on selected property (ID, Priority, Status, Responder).
        /// </summary>
        private void ApplyFilter()
        {
            var filteredList = new ObservableCollection<IncidentModel>();
            var filter = FilterText?.ToLowerInvariant() ?? "";

            // Always filter from the original collection
            foreach (var incident in _allIncidents)
            {
                bool isMatch = false;

                switch (SelectedFilterProperty)
                {
                    case "ID":
                        isMatch = incident.Id.ToString().Contains(filter);
                        break;
                    case "Priority":
                        isMatch = (incident.Priority?.ToLowerInvariant() ?? "").Contains(filter);
                        break;
                    case "Status":
                        isMatch = (incident.Status?.ToLowerInvariant() ?? "").Contains(filter);
                        break;
                    case "Responder":
                        isMatch = (incident.ResponderName?.ToLowerInvariant() ?? "").Contains(filter);
                        break;
                    default: // "All"
                        isMatch = incident.Id.ToString().Contains(filter) ||
                                 (incident.Priority?.ToLowerInvariant() ?? "").Contains(filter) ||
                                 (incident.Status?.ToLowerInvariant() ?? "").Contains(filter) ||
                                 (incident.ResponderName?.ToLowerInvariant() ?? "").Contains(filter) ||
                                 (incident.ResponderComments?.ToLowerInvariant() ?? "").Contains(filter);
                        break;
                }

                if (isMatch)
                {
                    filteredList.Add(incident);
                }
            }

            Incidents = filteredList;

            // Apply sort if there is an active sort property
            if (!string.IsNullOrEmpty(SortProperty))
            {
                ApplySorting(SortProperty, false);
            }
        }

        /// <summary>
        /// Handles the column header click event to sort incidents by the selected property.
        /// Toggles sort direction if the same property is clicked multiple times.
        /// </summary>
        /// <param name="propertyName">The name of the property to sort by</param>
        private void SortIncidents(string propertyName)
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
        /// Sorts the current collection of incidents based on the selected property.
        /// </summary>
        private void ApplySorting(string propertyName, bool updateIndicator)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return;

            // Build a sorted list based on property and direction
            IEnumerable<IncidentModel> sortedData;

            // Create appropriate sorting expression based on property name
            switch (propertyName)
            {
                case "Id":
                    sortedData = IsSortAscending ?
                        Incidents.OrderBy(i => i.Id) :
                        Incidents.OrderByDescending(i => i.Id);
                    break;
                case "Priority":
                    sortedData = IsSortAscending ?
                        Incidents.OrderBy(i => i.Priority) :
                        Incidents.OrderByDescending(i => i.Priority);
                    break;
                case "Status":
                    sortedData = IsSortAscending ?
                        Incidents.OrderBy(i => i.Status) :
                        Incidents.OrderByDescending(i => i.Status);
                    break;
                case "Responder":
                    sortedData = IsSortAscending ?
                        Incidents.OrderBy(i => i.ResponderName) :
                        Incidents.OrderByDescending(i => i.ResponderName);
                    break;
                case "ResolvedDate":
                    sortedData = IsSortAscending ?
                        Incidents.OrderBy(i => i.ResolvedDate) :
                        Incidents.OrderByDescending(i => i.ResolvedDate);
                    break;
                default:
                    return;
            }

            // Update the collection with sorted data
            Incidents = new ObservableCollection<IncidentModel>(sortedData);

            // Update sort indicator
            if (updateIndicator)
            {
                SortIndicator = IsSortAscending ? "▲" : "▼";

                // Notify that all sort indicators may have changed
                OnPropertyChanged(nameof(IdSortIndicator));
                OnPropertyChanged(nameof(PrioritySortIndicator));
                OnPropertyChanged(nameof(StatusSortIndicator));
                OnPropertyChanged(nameof(ResponderSortIndicator));
                OnPropertyChanged(nameof(ResolvedDateSortIndicator));
            }
        }

        /// <summary>
        /// Helper to generate appropriate sort indicators for the UI.
        /// Returns an arrow symbol when the column is the current sort column.
        /// </summary>
        /// <param name="columnName">The name of the column to check</param>
        /// <returns>A sort indicator arrow if this is the sort column; otherwise an empty string</returns>
        public string GetSortIndicator(string columnName)
        {
            if (columnName == SortProperty)
            {
                return IsSortAscending ? " ▲" : " ▼";
            }
            return string.Empty;
        }

        /// <summary>
        /// Gets the sort indicator for the ID column
        /// </summary>
        public string IdSortIndicator => GetSortIndicator("Id");

        /// <summary>
        /// Gets the sort indicator for the Priority column
        /// </summary>
        public string PrioritySortIndicator => GetSortIndicator("Priority");

        /// <summary>
        /// Gets the sort indicator for the Status column
        /// </summary>
        public string StatusSortIndicator => GetSortIndicator("Status");

        /// <summary>
        /// Gets the sort indicator for the Responder column
        /// </summary>
        public string ResponderSortIndicator => GetSortIndicator("Responder");

        /// <summary>
        /// Gets the sort indicator for the ResolvedDate column
        /// </summary>
        public string ResolvedDateSortIndicator => GetSortIndicator("ResolvedDate");
    }
}
