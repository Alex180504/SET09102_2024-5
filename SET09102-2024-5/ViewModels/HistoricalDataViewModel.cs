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

		private readonly IDataService _dataService;

		public ObservableCollection<EnvironmentalDataModel> DataPoints { get; set; } = new();

		private string selectedCategory = "";
		public string SelectedCategory
		{
			get => selectedCategory;
			set
			{
				selectedCategory = value;
				OnPropertyChanged();
				LoadHistoricalData();
			}
		}

		private string selectedSensorSite = "";
		public string SelectedSensorSite
		{
			get => selectedSensorSite;
			set
			{
				selectedSensorSite = value;
				OnPropertyChanged();
				LoadHistoricalData();
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

		// Default constructor for XAML instantiation
		public HistoricalDataViewModel() : this(new MockDataService()) { }

		public async void LoadHistoricalData()
		{
			if (string.IsNullOrEmpty(SelectedCategory) || string.IsNullOrEmpty(SelectedSensorSite))
				return;

			var results = await _dataService.GetHistoricalData(SelectedCategory, SelectedSensorSite);
			DataPoints.Clear();
			foreach (var item in results)
				DataPoints.Add(item);
		}
	}
}