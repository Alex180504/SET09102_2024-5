using System.Windows.Input;

namespace SET09102_2024_5.ViewModels
{
    public class MainPageViewModel : BaseViewModel
    {
        private int count;

        public MainPageViewModel()
        {
            IncrementCountCommand = new Command(OnIncrementCount);
            NavigateToSensorManagementCommand = new Command(OnNavigateToSensorManagement);

            NavigateToHistoricalDataCommand = new Command(OnNavigateToHistoricalData);
            NavigateToSensorMapCommand = new Command(OnNavigateToSensorMap);

        }

        public int Count
        {
            get => count;
            set
            {
                if (SetProperty(ref count, value))
                {
                    OnPropertyChanged(nameof(CounterText));
                }
            }
        }

        public string CounterText => Count == 1 ? $"Clicked {Count} time" : $"Clicked {Count} times";

        public ICommand IncrementCountCommand { get; }
        public ICommand NavigateToSensorManagementCommand { get; }

        public ICommand NavigateToHistoricalDataCommand { get; }
        public ICommand NavigateToSensorMapCommand { get; }

        private void OnIncrementCount()
        {
            Count++;
        }

        private async void OnNavigateToSensorManagement()
        {
            await Shell.Current.GoToAsync("SensorManagementPage");
        }

        private async void OnNavigateToHistoricalData()
        {
            await Shell.Current.GoToAsync("HistoricalDataPage");
        }

        private async void OnNavigateToSensorMap()
        {
            await Shell.Current.GoToAsync("MapPage");
        }
    }
}
