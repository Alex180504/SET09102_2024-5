using System.Collections.ObjectModel;
using System.Windows.Input;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.ViewModels
{
    public class MaintenanceListViewModel : BaseViewModel
    {
        private readonly IMaintenanceService _service;

        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public ObservableCollection<Maintenance> Tasks { get; } = new();

        // ← Use non-generic ICommand
        public ICommand LoadTasksCommand { get; }
        public ICommand AddTaskCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }

        public MaintenanceListViewModel(IMaintenanceService service)
        {
            _service = service;

            Title = "Maintenance Tasks";

            LoadTasksCommand = new Command(async () => await ExecuteLoad());
            AddTaskCommand = new Command(async () =>
                await Shell.Current.GoToAsync("MaintenanceDetailPage?maintenanceId=-1"));

            EditTaskCommand = new Command<Maintenance>(async m =>
                await Shell.Current.GoToAsync($"MaintenanceDetailPage?maintenanceId={m.MaintenanceId}"));

            DeleteTaskCommand = new Command<Maintenance>(async m =>
            {
                await _service.DeleteAsync(m.MaintenanceId);
                await ExecuteLoad();
            });

            // Initial load
            _ = ExecuteLoad();
        }

        private async Task ExecuteLoad()
        {
            if (IsBusy) return;
            IsBusy = true;

            Tasks.Clear();
            var list = await _service.GetAllAsync();
            foreach (var m in list)
            {
                m.Sensor.DisplayName = $"{m.Sensor.SensorType} (ID {m.Sensor.SensorId})";
                m.Maintainer.DisplayName = $"{m.Maintainer.FirstName} {m.Maintainer.LastName}";
                Tasks.Add(m);
            }
            IsBusy = false;
        }
    }
}
