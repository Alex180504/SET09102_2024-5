using SET09102_2024_5.Features.HistoricalData.ViewModels;
using Microsoft.Maui.Controls;
using SET09102_2024_5.Models;
using System.Linq;

namespace SET09102_2024_5.Features.HistoricalData
{
    public partial class HistoricalDataPage : ContentPage
    {
        HistoricalDataViewModel vm;
        bool webViewLoaded = false;

        public HistoricalDataPage()
        {
            InitializeComponent();
            vm = new HistoricalDataViewModel();
            BindingContext = vm;
            // pick the first category & site automatically:
            vm.SelectedCategory = vm.Categories.First();
            vm.SelectedSensorSite = vm.SensorSites.First();
            vm.SelectedParameter = vm.ParameterTypes.First();

            ChartWebView.Source = "chart.html";

            ChartWebView.Navigated += (s, e) =>
            {
                webViewLoaded = true;
                if (vm.DataPoints.Any()) { InjectData(); };
            };
            vm.DataPoints.CollectionChanged += (s, e) =>
            {
                if (webViewLoaded) { InjectData(); }     
            };
            // re-draw when the user changes parameter
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.SelectedParameter) && webViewLoaded)
                    InjectData();
            };

            // Kick off the first load
            vm.LoadHistoricalData();
        }
        async void InjectData()
        {
            var param = vm.SelectedParameter;
            var data = vm.DataPoints.Select(dp => new{
                timestamp = dp.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                value = dp.Values.TryGetValue(param, out var v) ? v : 0
            });
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            //await ChartWebView.EvaluateJavaScriptAsync($"window.seriesLabel = \"{vm.SelectedParameter}\";");
            await ChartWebView.EvaluateJavaScriptAsync($"window.seriesLabel = {System.Text.Json.JsonSerializer.Serialize(vm.SelectedParameter)};");
            await ChartWebView.EvaluateJavaScriptAsync($"window.dotnetData={json};");
            await ChartWebView.EvaluateJavaScriptAsync("renderChart();");
        }

    }
}
