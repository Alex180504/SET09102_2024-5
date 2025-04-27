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
    public class SensorIncidentLogViewModel : BaseViewModel, IQueryAttributable
    {
        private readonly SensorMonitoringContext _context;
        private readonly IMainThreadService _mainThreadService;
        private readonly IDialogService _dialogService;

        private int _sensorId;
        private string _sensorInfo;
        private ObservableCollection<IncidentModel> _incidents;
        private IncidentModel _selectedIncident;
        private string _filterText;
        private bool _isLoading;
        private string _selectedFilterProperty;
        private List<string> _filterProperties;
        private string _sortProperty;
        private bool _isSortAscending = true;
        private string _sortIndicator;

        public int SensorId
        {
            get => _sensorId;
            set => SetProperty(ref _sensorId, value);
        }

        public string SensorInfo
        {
            get => _sensorInfo;
            set => SetProperty(ref _sensorInfo, value);
        }

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

        public IncidentModel SelectedIncident
        {
            get => _selectedIncident;
            set => SetProperty(ref _selectedIncident, value);
        }

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

        public bool HasIncidents => Incidents != null && Incidents.Count > 0;
        public bool HasNoIncidents => !HasIncidents;

        public ICommand LoadIncidentsCommand { get; }
        public ICommand ApplyCommand { get; }
        public ICommand SortCommand { get; }
        public ICommand BackCommand { get; }

        public SensorIncidentLogViewModel(
            SensorMonitoringContext context,
            IMainThreadService mainThreadService = null,
            IDialogService dialogService = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mainThreadService = mainThreadService ?? new Services.MainThreadService();
            _dialogService = dialogService ?? new Services.DialogService();

            // Initialize commands
            LoadIncidentsCommand = new Command(async () => await LoadIncidentsAsync(), () => !IsLoading);
            ApplyCommand = new Command(ApplyFilterAndRefresh, () => !IsLoading);
            SortCommand = new Command<string>(SortIncidents);
            BackCommand = new Command(async () => await Shell.Current.GoToAsync(".."));

            // Initialize collections
            Incidents = new ObservableCollection<IncidentModel>();
            FilterProperties = new List<string> { "All", "ID", "Priority", "Status", "Responder" };
            SelectedFilterProperty = "All";
            SortProperty = "";
            SortIndicator = "";
        }

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

                    Incidents = incidentModels;

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
                    Incidents = new ObservableCollection<IncidentModel>();
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ApplyFilterAndRefresh()
        {
            if (string.IsNullOrWhiteSpace(FilterText))
            {
                await LoadIncidentsAsync();
                return;
            }

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var filteredList = new ObservableCollection<IncidentModel>();
            var filter = FilterText?.ToLowerInvariant() ?? "";

            foreach (var incident in _incidents)
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
        public string PrioritySortIndicator => GetSortIndicator("Priority");
        public string StatusSortIndicator => GetSortIndicator("Status");
        public string ResponderSortIndicator => GetSortIndicator("Responder");
        public string ResolvedDateSortIndicator => GetSortIndicator("ResolvedDate");
    }

}
