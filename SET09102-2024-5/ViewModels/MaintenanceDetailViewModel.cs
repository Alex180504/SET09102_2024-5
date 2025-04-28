using System.Collections.ObjectModel;
using System.Windows.Input;
using SET09102_2024_5.Data.Repositories;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.ViewModels
{
    [QueryProperty(nameof(MaintenanceId), "maintenanceId")]
    public class MaintenanceDetailViewModel : BaseViewModel
    {
        private readonly IMaintenanceService _maintenanceSvc;
        private readonly ISensorService _sensorSvc;
        private readonly IRepository<User> _userRepo;

        // Backing fields + bindable props
        private DateTime _maintenanceDate = DateTime.Now;
        public DateTime MaintenanceDate
        {
            get => _maintenanceDate;
            set => SetProperty(ref _maintenanceDate, value);
        }

        private string _comments;
        public string Comments
        {
            get => _comments;
            set => SetProperty(ref _comments, value);
        }

        private int _selectedSensorId;
        public int SelectedSensorId
        {
            get => _selectedSensorId;
            set => SetProperty(ref _selectedSensorId, value);
        }

        private int _selectedMaintainerId;
        public int SelectedMaintainerId
        {
            get => _selectedMaintainerId;
            set => SetProperty(ref _selectedMaintainerId, value);
        }

        public ObservableCollection<Sensor> Sensors { get; } = new();
        public ObservableCollection<User> Users { get; } = new();

        public ICommand SaveCommand { get; }

        private int _maintenanceId;
        public int MaintenanceId
        {
            get => _maintenanceId;
            set
            {
                if (value == -1)
                {
                    _ = LoadAsync(null);
                }
                else if (SetProperty(ref _maintenanceId, value))
                    _ = LoadAsync(value);      // trigger load as soon as Shell injects the id
            }
        }
        public Sensor SelectedSensor { get; set; }
        public User SelectedMaintainer { get; set; }

        public MaintenanceDetailViewModel(
            IMaintenanceService svc,
            ISensorService sensorSvc,
            IRepository<User> userRepo
        )
        {
            _maintenanceSvc = svc;
            _sensorSvc = sensorSvc;
            _userRepo = userRepo;

            SaveCommand = new Command(async () => await ExecuteSave());

        }
        private async Task LoadAsync(int? id)
        {
            try
            {
                // Load sensors & users
                Sensors.Clear();
                foreach (var s in await _sensorSvc.GetAllWithConfigurationAsync())
                {
                    s.DisplayName = $"{s.SensorType} (ID {s.SensorId})";
                    Sensors.Add(s);
                }

                Users.Clear();
                foreach (var u in await _userRepo.GetAllAsync())
                {
                    u.DisplayName = $"{u.FirstName} {u.LastName}";
                    Users.Add(u);
                }

                // Load the maintenance record
                if (id != null)
                {
                    var m = await _maintenanceSvc.GetByIdAsync(id.Value);
                    if (m is null) return;

                    MaintenanceDate = m.MaintenanceDate ?? DateTime.Now;
                    Comments = m.MaintainerComments;
                    SelectedSensor = Sensors.FirstOrDefault(x => x.SensorId == m.SensorId);
                    SelectedMaintainer = Users.FirstOrDefault(x => x.UserId == m.MaintainerId);

                }
                // notify pickers
                OnPropertyChanged(nameof(SelectedSensor));
                OnPropertyChanged(nameof(SelectedMaintainer));
            }
            catch (Exception ex)
            {
                // Log or show the actual exception
                System.Diagnostics.Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async Task ExecuteSave()
        {
            // 1) Validation
            if (SelectedSensor == null)
            {
                await Shell.Current.DisplayAlert("Validation error",
                    "Please select a sensor before saving.", "OK");
                return;
            }
            if (SelectedMaintainer == null)
            {
                await Shell.Current.DisplayAlert("Validation error",
                    "Please select a maintainer before saving.", "OK");
                return;
            }

            // 2) Build the Maintenance DTO
            var m = new Maintenance
            {
                MaintenanceDate = MaintenanceDate,
                MaintainerComments = Comments,
                SensorId = SelectedSensor.SensorId,
                MaintainerId = SelectedMaintainer.UserId
            };

            if (MaintenanceId != 0)
                m.MaintenanceId = MaintenanceId;

            try
            {
                // 3) Save via service
                if (MaintenanceId == 0)
                    await _maintenanceSvc.ScheduleAsync(m);
                else
                    await _maintenanceSvc.UpdateAsync(m);

                // 4) Show success message
                await Shell.Current.DisplayAlert("Success",
                    "Maintenance task saved successfully.", "OK");

                // 5) Go back to the Maintenance list
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                // 6) On error, show it
                await Shell.Current.DisplayAlert("Error",
                    $"Failed to save maintenance: {ex.Message}", "OK");
            }
        }


    }
}
