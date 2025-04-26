using SET09102_2024_5.Features.HistoricalData.ViewModels;
using SET09102_2024_5.Services;
using Microsoft.Maui.Controls;
using SET09102_2024_5.ViewModels;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Features.HistoricalData
{
    public partial class HistoricalDataPage : ContentPage
    {
        public HistoricalDataPage()
        {
            InitializeComponent();
            BindingContext = new HistoricalDataViewModel(new MockDataService());
        }
    }
}
