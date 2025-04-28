using Microsoft.EntityFrameworkCore;
using SET09102_2024_5.Data;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using static Microsoft.Maui.ApplicationModel.Permissions;

namespace SET09102_2024_5.ViewModels
{
    /// <summary>
    /// ViewModel for managing sensor configuration settings
    /// </summary>
    /// <remarks>
    /// Provides functionality for searching, viewing, and editing sensor configurations.
    /// Includes validation and persistence of sensor settings.
    /// </remarks>
    public class SensorManagementViewModel : BaseViewModel
    {
        private readonly SensorMonitoringContext _context;
        private readonly IMainThreadService _mainThreadService;
        private readonly IDialogService _dialogService;

        private Sensor _selectedSensor;
        private ObservableCollection<Sensor> _sensors;
        private ObservableCollection<Sensor> _filteredSensors;
        private Configuration _configuration;
        private SensorFirmware _firmware;
        private bool _isSensorSelected;
        private bool _isLoading;
        private bool _isSearching;
        private string _searchText;

        /// Firm update related fields
        private string _firmwareVersion;
        private DateTime? _lastUpdateDate;

        private List<string> _statusOptions = new List<string> { "Active", "Inactive", "Maintenance", "Error" };

        // Validation related fields
        private Dictionary<string, string> _validationErrors = new Dictionary<string, string>();
        private bool _hasValidationErrors;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorManagementViewModel"/> class
        /// </summary>
        /// <param name="context">Database context for sensor data access</param>
        /// <param name="mainThreadService">Service for executing code on the main thread</param>
        /// <param name="dialogService">Service for displaying dialogs to the user</param>
        /// <exception cref="ArgumentNullException">Thrown when context is null</exception>
        public SensorManagementViewModel(SensorMonitoringContext context, IMainThreadService mainThreadService = null, IDialogService dialogService = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mainThreadService = mainThreadService ?? new Services.MainThreadService();
            _dialogService = dialogService ?? new Services.DialogService();

            // Init commands
            LoadSensorsCommand = new Command(async () => await LoadSensorsAsync(), () => !IsLoading);
            SaveChangesCommand = new Command(async () => await SaveChangesAsync(),
                () => !IsLoading && SelectedSensor != null && !HasValidationErrors);
            SearchCommand = new Command(ExecuteSearch);
            ClearSearchCommand = new Command(ClearSearch);
            ValidateCommand = new Command<string>(ValidateField);

            // Init collections
            Sensors = new ObservableCollection<Sensor>();
            FilteredSensors = new ObservableCollection<Sensor>();

            // Init async
            _mainThreadService.BeginInvokeOnMainThread(async () => await InitializeAsync());
        }


        public string FirmwareVersion
        {
            get => _firmwareVersion;
            set
            {
                if (SetProperty(ref _firmwareVersion, value))
                {
                    if (FirmwareInfo != null)
                        FirmwareInfo.FirmwareVersion = value;
                    OnPropertyChanged(nameof(FirmwareInfo));
                }
            }
        }
        public DateTime? LastUpdateDate
        {
            get => _lastUpdateDate;
            set => SetProperty(ref _lastUpdateDate, value);
        }

        public async Task InitializeAsync()
        {
            await LoadSensorsAsync();
        }

        /// <summary>
        /// Indicates whether data loading operations are in progress
        /// Controls command availability through CanExecute
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    (LoadSensorsCommand as Command)?.ChangeCanExecute();
                    (SaveChangesCommand as Command)?.ChangeCanExecute();
                }
            }
        }

        /// <summary>
        /// Gets or sets the collection of all sensors
        /// </summary>
        public ObservableCollection<Sensor> Sensors
        {
            get => _sensors;
            set => SetProperty(ref _sensors, value);
        }

        /// <summary>
        /// Gets or sets the collection of filtered sensors based on search criteria
        /// </summary>
        public ObservableCollection<Sensor> FilteredSensors
        {
            get => _filteredSensors;
            set => SetProperty(ref _filteredSensors, value);
        }

        /// <summary>
        /// Gets or sets the currently selected sensor
        /// When changed, loads the sensor's configuration details
        /// </summary>
        public Sensor SelectedSensor
        {
            get => _selectedSensor;
            set
            {
                if (SetProperty(ref _selectedSensor, value))    // Load sensor details when selected or reset all fields if null
                {
                    IsSensorSelected = _selectedSensor != null;
                    if (_selectedSensor != null)
                    {
                        LoadSensorDetailsAsync();
                        if (IsSearchActive)
                        {
                            HideSearchResults();
                        }
                    }
                    else
                    {
                        Configuration = null;
                        FirmwareInfo = null;
                    }

                    ClearValidationErrors();
                    (SaveChangesCommand as Command)?.ChangeCanExecute();
                }
            }
        }

        /// <summary>
        /// Gets or sets the search text for filtering sensors
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether search results are being displayed
        /// </summary>
        public bool IsSearchActive
        {
            get => _isSearching;
            set => SetProperty(ref _isSearching, value);
        }

        /// <summary>
        /// Gets or sets the configuration for the selected sensor
        /// </summary>
        public Configuration Configuration
        {
            get => _configuration;
            set => SetProperty(ref _configuration, value);
        }

        /// <summary>
        /// Gets or sets the firmware information for the selected sensor
        /// </summary>
        public SensorFirmware FirmwareInfo
        {
            get => _firmware;
            set => SetProperty(ref _firmware, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether a sensor is currently selected
        /// </summary>
        public bool IsSensorSelected
        {
            get => _isSensorSelected;
            set => SetProperty(ref _isSensorSelected, value);
        }

        /// <summary>
        /// Gets the list of available status options for sensors
        /// </summary>
        public List<string> StatusOptions => _statusOptions;

        /// <summary>
        /// Indicates if any validation errors exist that prevent saving changes to DB
        /// </summary>
        public bool HasValidationErrors
        {
            get => _hasValidationErrors;
            set
            {
                if (SetProperty(ref _hasValidationErrors, value))
                {
                    (SaveChangesCommand as Command)?.ChangeCanExecute();
                }
            }
        }

        /// <summary>
        /// Gets the dictionary of validation errors keyed by field name
        /// </summary>
        public Dictionary<string, string> ValidationErrors => _validationErrors;

        /// <summary>
        /// Gets the command to load sensors from the database
        /// </summary>
        public ICommand LoadSensorsCommand { get; }

        /// <summary>
        /// Gets the command to save changes to the selected sensor
        /// </summary>
        public ICommand SaveChangesCommand { get; }

        /// <summary>
        /// Gets the command to execute a search based on the current SearchText
        /// </summary>
        public ICommand SearchCommand { get; }

        /// <summary>
        /// Gets the command to clear the current search
        /// </summary>
        public ICommand ClearSearchCommand { get; }

        /// <summary>
        /// Gets the command to validate a specific field
        /// </summary>
        public ICommand ValidateCommand { get; }

        /// <summary>
        /// Filters sensors based on search text across DisplayName, SensorType, and Measurand
        /// </summary>
        /// <param name="searchText">The search text to filter by</param>
        public void FilterSensors(string searchText)
        {
            // Always show the filtered list when search bar is active
            IsSearchActive = true;
            FilteredSensors.Clear();

            // If search is empty, show all sensors
            if (string.IsNullOrWhiteSpace(searchText))
            {
                foreach (var sensor in Sensors)
                {
                    FilteredSensors.Add(sensor);
                }
                return;
            }

            // Otherwise, filter by the search text
            var lowerSearchText = searchText.ToLowerInvariant();
            var filtered = Sensors.Where(s =>
                (s.DisplayName?.ToLowerInvariant().Contains(lowerSearchText) ?? false) ||
                (s.SensorType?.ToLowerInvariant().Contains(lowerSearchText) ?? false) ||
                (s.Measurand?.QuantityName?.ToLowerInvariant().Contains(lowerSearchText) ?? false)
            ).ToList();

            foreach (var sensor in filtered)
            {
                FilteredSensors.Add(sensor);
            }
        }

        /// <summary>
        /// Executes the search using the current SearchText
        /// </summary>
        private void ExecuteSearch()
        {
            FilterSensors(SearchText);
        }

        /// <summary>
        /// Hides the search results and returns to the main view
        /// </summary>
        public void HideSearchResults()
        {
            IsSearchActive = false;
        }

        /// <summary>
        /// Clears the current search text and hides search results
        /// </summary>
        private void ClearSearch()
        {
            SearchText = string.Empty;
            HideSearchResults();
        }

        /// <summary>
        /// Shows all sensors in the search dropdown
        /// Used when the search bar receives focus and no text is entered
        /// </summary>
        public void ShowAllSensorsInSearch()
        {
            IsSearchActive = true;
            FilteredSensors.Clear();
            foreach (var sensor in Sensors)
            {
                FilteredSensors.Add(sensor);
            }
        }

        /// <summary>
        /// Special property for handling orientation setting with degree symbol
        /// Parses user input and updates Configuration.Orientation accordingly
        /// </summary>
        public string OrientationText
        {
            get => Configuration?.Orientation?.ToString() ?? string.Empty;
            set
            {
                if (Configuration == null) return;

                // Try to parse the input as an integer
                if (string.IsNullOrWhiteSpace(value))
                {
                    Configuration.Orientation = null;
                }
                else if (int.TryParse(value.TrimEnd('°'), out int degrees))
                {
                    Configuration.Orientation = degrees;
                }

                OnPropertyChanged(nameof(OrientationText));
            }
        }

        /// <summary>
        /// Validates a specific configuration field and updates validation errors
        /// Called when a field loses focus in the UI
        /// </summary>
        /// <param name="fieldName">The name of the field to validate</param>
        private void ValidateField(string fieldName)
        {
            // Don't proceed with validation if Configuration is null (no sensor selected)
            if (Configuration == null) return;

            ClearValidationError(fieldName);

            switch (fieldName)
            {
                case nameof(Configuration.Latitude):
                    ValidateLatitude();
                    break;
                case nameof(Configuration.Longitude):
                    ValidateLongitude();
                    break;
                case nameof(Configuration.Altitude):
                    ValidateAltitude();
                    break;
                case nameof(Configuration.Orientation):
                    ValidateOrientation();
                    break;
                case nameof(Configuration.MeasurementFrequency):
                    ValidateMeasurementFrequency();
                    break;
                case nameof(Configuration.MinThreshold):
                    ValidateMinThreshold();
                    break;
                case nameof(Configuration.MaxThreshold):
                    ValidateMaxThreshold();
                    break;
            }
            HasValidationErrors = ValidationErrors.Count > 0;
        }

        private void ValidateLatitude()
        {
            if (Configuration == null || !Configuration.Latitude.HasValue) return;

            if (Configuration.Latitude < -90 || Configuration.Latitude > 90)
            {
                AddValidationError(nameof(Configuration.Latitude), "Latitude must be between -90 and 90 degrees");
            }
        }

        private void ValidateLongitude()
        {
            if (Configuration == null || !Configuration.Longitude.HasValue) return;

            if (Configuration.Longitude < -180 || Configuration.Longitude > 180)
            {
                AddValidationError(nameof(Configuration.Longitude), "Longitude must be between -180 and 180 degrees");
            }
        }

        private void ValidateAltitude()
        {
            if (Configuration == null || !Configuration.Altitude.HasValue) return;

            if (Configuration.Altitude < -11000 || Configuration.Altitude > 10000)
            {
                AddValidationError(nameof(Configuration.Altitude), "Altitude must be a reasonable value (-11,000 to 10,000 meters)");
            }
        }

        private void ValidateOrientation()
        {
            if (Configuration == null) return;

            if (!Configuration.Orientation.HasValue)
            {
                AddValidationError(nameof(Configuration.Orientation), "Orientation is required");
                return;
            }

            if (Configuration.Orientation < 0 || Configuration.Orientation > 359)
            {
                AddValidationError(nameof(Configuration.Orientation), "Orientation must be a value between 0 and 359 degrees");
            }
        }

        private void ValidateMeasurementFrequency()
        {
            if (Configuration == null || !Configuration.MeasurementFrequency.HasValue) return;

            if (Configuration.MeasurementFrequency <= 0)
            {
                AddValidationError(nameof(Configuration.MeasurementFrequency), "Measurement frequency must be greater than 0");
            }
        }

        private void ValidateMinThreshold()
        {
            if (Configuration == null || !Configuration.MinThreshold.HasValue) return;

            if (Configuration.MaxThreshold.HasValue &&
                Configuration.MinThreshold > Configuration.MaxThreshold)
            {
                AddValidationError(nameof(Configuration.MinThreshold), "Minimum threshold cannot be greater than maximum threshold");
            }
        }

        private void ValidateMaxThreshold()
        {
            if (Configuration == null || !Configuration.MaxThreshold.HasValue) return;

            if (Configuration.MinThreshold.HasValue &&
                Configuration.MaxThreshold < Configuration.MinThreshold)
            {
                AddValidationError(nameof(Configuration.MaxThreshold), "Maximum threshold cannot be less than minimum threshold");
            }
        }

        /// <summary>
        /// Adds a validation error for the specified property
        /// </summary>
        /// <param name="propertyName">The name of the property with the error</param>
        /// <param name="errorMessage">The error message</param>
        private void AddValidationError(string propertyName, string errorMessage)
        {
            if (_validationErrors.ContainsKey(propertyName))
            {
                _validationErrors[propertyName] = errorMessage;
            }
            else
            {
                _validationErrors.Add(propertyName, errorMessage);
            }

            OnPropertyChanged(nameof(ValidationErrors));
        }

        /// <summary>
        /// Clears the validation error for a specific property
        /// </summary>
        /// <param name="propertyName">The name of the property to clear errors for</param>
        private void ClearValidationError(string propertyName)
        {
            if (_validationErrors.ContainsKey(propertyName))
            {
                _validationErrors.Remove(propertyName);
                OnPropertyChanged(nameof(ValidationErrors));
            }
        }

        /// <summary>
        /// Clears all validation errors
        /// </summary>
        private void ClearValidationErrors()
        {
            _validationErrors.Clear();
            HasValidationErrors = false;
            OnPropertyChanged(nameof(ValidationErrors));
        }

        /// <summary>
        /// Validates all configuration fields
        /// </summary>
        private void ValidateAllFields()
        {
            ValidateField(nameof(Configuration.Latitude));
            ValidateField(nameof(Configuration.Longitude));
            ValidateField(nameof(Configuration.Altitude));
            ValidateField(nameof(Configuration.Orientation));
            ValidateField(nameof(Configuration.MeasurementFrequency));
            ValidateField(nameof(Configuration.MinThreshold));
            ValidateField(nameof(Configuration.MaxThreshold));
        }

        /// <summary>
        /// Loads all sensors from the database
        /// Uses AsNoTracking for better performance when only reading data
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        private async Task LoadSensorsAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;

                var sensors = await _context.Sensors
                    .Include(s => s.Measurand)
                    .AsNoTracking()
                    .ToListAsync();

                _mainThreadService.BeginInvokeOnMainThread(() =>
                {
                    Sensors.Clear();
                    foreach (var sensor in sensors)
                    {
                        // Generate display name
                        if (string.IsNullOrEmpty(sensor.DisplayName))
                        {
                            sensor.DisplayName = $"{sensor.SensorId} - {sensor.SensorType}";
                        }
                        Sensors.Add(sensor);
                    }

                    if (IsSearchActive)
                    {
                        FilterSensors(SearchText);
                    }
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
        /// Loads detailed configuration for the selected sensor
        /// Creates a default configuration if none is found
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        private async Task LoadSensorDetailsAsync()
        {
            if (SelectedSensor == null) return;

            try
            {
                IsLoading = true;

                var sensor = await _context.Sensors
                    .Include(s => s.Configuration)
                    .Include(s => s.Firmware)
                    .Include(s => s.Measurand)
                    .FirstOrDefaultAsync(s => s.SensorId == SelectedSensor.SensorId);

                if (sensor != null)
                {
                    Configuration = sensor.Configuration ?? new Configuration
                    {
                        SensorId = sensor.SensorId,
                        Latitude = 0,
                        Longitude = 0,
                        Altitude = 0,
                        Orientation = 0,
                        MeasurementFrequency = 5,
                        MinThreshold = 0,
                        MaxThreshold = 1
                    };

                    FirmwareInfo = sensor.Firmware;
                    FirmwareVersion = sensor.Firmware?.FirmwareVersion ?? string.Empty;
                    LastUpdateDate = sensor.Firmware?.LastUpdateDate ?? DateTime.Now;
                }

                OnPropertyChanged(nameof(Configuration));
                OnPropertyChanged(nameof(FirmwareInfo));
                OnPropertyChanged(nameof(OrientationText));
            }
            catch (Exception ex)
            {
                await _dialogService.DisplayErrorAsync($"Failed to load sensor details: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Saves configuration changes to the database
        /// Validates all fields before saving and shows confirmation dialog
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        private async Task SaveChangesAsync()
        {
            if (SelectedSensor == null || IsLoading) return;

            ValidateAllFields();

            if (HasValidationErrors)
            {
                var errors = string.Join("\n", ValidationErrors.Values);
                await _dialogService.DisplayErrorAsync($"Please correct the following errors before saving:\n{errors}", "Validation Errors");
                return;
            }

            bool confirmSave = await _dialogService.DisplayConfirmationAsync(
                "Confirm Save",
                "Are you sure you want to save these changes?",
                "Yes", "No");

            if (!confirmSave)
            {
                return;
            }

            try
            {
                IsLoading = true;

                var sensor = await _context.Sensors
                    .Include(s => s.Configuration)
                    .FirstOrDefaultAsync(s => s.SensorId == SelectedSensor.SensorId);

                if (sensor != null)
                {
                    sensor.SensorType = SelectedSensor.SensorType;
                    sensor.Status = SelectedSensor.Status;
                    if (sensor.Configuration == null)
                    {
                        // Create new configuration if it doesn't exist
                        sensor.Configuration = Configuration;
                        _context.Add(Configuration);
                    }
                    else
                    {
                        // Update existing configuration
                        sensor.Configuration.Latitude = Configuration.Latitude;
                        sensor.Configuration.Longitude = Configuration.Longitude;
                        sensor.Configuration.Altitude = Configuration.Altitude;
                        sensor.Configuration.Orientation = Configuration.Orientation;
                        sensor.Configuration.MeasurementFrequency = Configuration.MeasurementFrequency;
                        sensor.Configuration.MinThreshold = Configuration.MinThreshold;
                        sensor.Configuration.MaxThreshold = Configuration.MaxThreshold;
                    }

                    if (sensor.Firmware == null)
                    {
                        sensor.Firmware = new SensorFirmware { SensorId = sensor.SensorId };
                        _context.Add(sensor.Firmware);
                    }
                    sensor.Firmware.FirmwareVersion = FirmwareVersion;
                    sensor.Firmware.LastUpdateDate = LastUpdateDate;

                    // Save changes
                    await _context.SaveChangesAsync();
                    await LoadSensorsAsync();
                    await _dialogService.DisplaySuccessAsync("Sensor settings saved successfully.");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.DisplayErrorAsync($"Failed to save sensor settings: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
