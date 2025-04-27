using System.Collections.ObjectModel;
using System.Windows.Input;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;
using Microsoft.Maui.Controls;

namespace SET09102_2024_5.ViewModels
{
    public class DataQualityViewModel : BindableObject
    {
        private readonly IDataQualityService _svc;
        public ObservableCollection<MissingRecord> Missing { get; } = new();
        public ObservableCollection<OutOfRangeRecord> OutOfRange { get; } = new();
        public ObservableCollection<DuplicateRecord> Duplicates { get; } = new();

        public DateTime FromDate { get; set; } = DateTime.Now.AddDays(-7);
        public DateTime ToDate { get; set; } = DateTime.Now;
        public string SelectedCategory { get; set; }
        public string SelectedSite { get; set; }

        public ICommand RunChecksCommand { get; }

        public DataQualityViewModel(IDataQualityService svc)
        {
            _svc = svc;
            RunChecksCommand = new Command(async () => await RunChecks());
        }

        private async Task RunChecks()
        {
            var rpt = await _svc.RunChecksAsync(SelectedCategory, SelectedSite, FromDate, ToDate);
            Missing.Clear(); rpt.Missing.ForEach(Missing.Add);
            OutOfRange.Clear(); rpt.OutOfRange.ForEach(OutOfRange.Add);
            Duplicates.Clear(); rpt.Duplicates.ForEach(Duplicates.Add);
        }
    }
}
