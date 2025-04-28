using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SET09102_2024_5.Models;
using SET09102_2024_5.Services;

namespace SET09102_2024_5.Features.HistoricalData.ViewModels
{
	public class HistoricalDataViewModel : INotifyPropertyChanged
	{
        public List<string> Categories { get; } = new() { "Air", "Water", "Weather" };
		public List<string> SensorSites { get; } = new() { "Site A", "Site B", "Site C" };
        // Static lists per category
        private static readonly Dictionary<string, List<string>> _paramsByCategory = new()
        {
            ["Air"] = new List<string>
            {
                "Nitrogen dioxide",
                "Sulphur dioxide",
                "PM2.5 particulate matter (Hourly measured)",
                "PM10 particulate matter (Hourly measured)"
            },
            ["Water"] = new List<string>
            {
                    "Nitrate (mg l-1)",
                    "Nitrite <mg l-1)",
                    "Phosphate (mg l-1)"
            },
            ["Weather"] = new List<string>
            {
                "temperature_2m (¬∞C)",
                "relative_humidity_2m (%)",
                "wind_speed_10m (m/s)",
                "wind_direction_10m (¬∞)"
            }
        };

        // Backing for the currently displayed list
        private List<string> _parameterTypes = new();
        public List<string> ParameterTypes
        {
            get => _parameterTypes;
            private set { _parameterTypes = value; OnPropertyChanged(); }
        }

        private readonly IDataService _dataService;

		public ObservableCollection<EnvironmentalDataModel> DataPoints { get; set; } = new();

        private string selectedCategory = "Air";
        public string SelectedCategory
		{
			get => selectedCategory;
			set
			{
				selectedCategory = value;
                OnPropertyChanged();

                // swap in the right list of parameters
                ParameterTypes = _paramsByCategory[selectedCategory];
                // default-pick the first one so SelectedParameter isn’t null
                SelectedParameter = ParameterTypes.First();

                _ = LoadHistoricalData();
            }
		}
        private string selectedSensorSite = "Site A";
        public string SelectedSensorSite
        {
            get => selectedSensorSite;
            set
            {
                if (selectedSensorSite == value)
                    return;
                selectedSensorSite = value;
                OnPropertyChanged();
                _ = LoadHistoricalData();
            }
        }
        private string _selectedParameter;
        public string SelectedParameter
        {
            get => _selectedParameter;
            set
            {
                if (_selectedParameter == value) return;
                _selectedParameter = value;
                OnPropertyChanged();
                _ = LoadHistoricalData();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		// Main constructor with dependency injection
		public HistoricalDataViewModel(IDataService dataService)
		{
			_dataService = dataService;
		}

		public HistoricalDataViewModel() : this(new MockDataService())
        {
            // seed the parameters for the default category
            ParameterTypes = _paramsByCategory[selectedCategory];
            SelectedParameter = ParameterTypes.First();

            // one‐time initial load
            _ = LoadHistoricalData();
        }

        public async Task LoadHistoricalData()
        {
            if (string.IsNullOrEmpty(SelectedCategory)) return;
            var results = await _dataService.GetHistoricalData(SelectedCategory, null);
            DataPoints.Clear();
            foreach (var item in results)
                DataPoints.Add(item);
        }
    }
}