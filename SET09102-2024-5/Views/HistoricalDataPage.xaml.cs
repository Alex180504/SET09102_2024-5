using SET09102_2024_5.Features.HistoricalData.ViewModels;
using SET09102_2024_5.Services;
using Microsoft.Maui.Controls;
using SET09102_2024_5.ViewModels;
using SET09102_2024_5.Models;
using System.Reflection;

namespace SET09102_2024_5.Features.HistoricalData
{
    public partial class HistoricalDataPage : ContentPage
    {
        HistoricalDataViewModel vm;
        bool webViewLoaded = false;

        public HistoricalDataPage()
        {
            InitializeComponent();
            vm = (HistoricalDataViewModel)BindingContext;
            ChartWebView.Source = "chart.html";
            ChartWebView.Navigated += (s, e) => { webViewLoaded = true; if (vm.DataPoints.Any()) InjectData(); };
            vm.DataPoints.CollectionChanged += (s, e) => { if (webViewLoaded) InjectData(); };
        }
        async void InjectData()
        {
            var data = vm.DataPoints.Select(dp => new { timestamp = dp.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                value1 = dp.Value1,
                value2 = dp.Value2,
                value3 = dp.Value3,
                value4 = dp.Value4,
            });
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            await ChartWebView.EvaluateJavaScriptAsync($"window.dotnetData={json};");
            await ChartWebView.EvaluateJavaScriptAsync("renderChart();");
        }

    }
}
