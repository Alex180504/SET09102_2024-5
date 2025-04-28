using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using SET09102_2024_5.Features.HistoricalData.ViewModels;

namespace SET09102_2024_5.Views
{
    public partial class HistoricalDataPage : ViewBase
    {
        private HistoricalDataViewModel ViewModel => BindingContext as HistoricalDataViewModel;
        private bool webViewLoaded = false;

        // Add parameterless constructor for Shell navigation
        public HistoricalDataPage()
        {
            InitializeComponent();
            BindingContext = App.Current.Handler.MauiContext?.Services.GetService<HistoricalDataViewModel>();
            InitializeViewModel();
        }

        public HistoricalDataPage(HistoricalDataViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            if (ViewModel != null)
            {
                // pick the first category & site automatically:
                ViewModel.SelectedCategory = ViewModel.Categories.FirstOrDefault();
                ViewModel.SelectedSensorSite = ViewModel.SensorSites.FirstOrDefault();
                ViewModel.SelectedParameter = ViewModel.ParameterTypes.FirstOrDefault();
                
                ChartWebView.Source = "chart.html";

                ChartWebView.Navigated += (s, e) =>
                {
                    webViewLoaded = true;
                    if (ViewModel.DataPoints.Any()) { InjectData(); };
                };
                
                ViewModel.DataPoints.CollectionChanged += (s, e) =>
                {
                    if (webViewLoaded) { InjectData(); }     
                };
                
                // re-draw when the user changes parameter
                ViewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ViewModel.SelectedParameter) && webViewLoaded)
                        InjectData();
                };

                // Kick off the first load
                ViewModel.LoadHistoricalData();
            }
        }

        async void InjectData()
        {
            if (ViewModel == null) return;
            
            var param = ViewModel.SelectedParameter;
            var data = ViewModel.DataPoints.Select(dp => new{
                timestamp = dp.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                value = dp.Values.TryGetValue(param, out var v) ? v : 0
            });
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            await ChartWebView.EvaluateJavaScriptAsync($"window.seriesLabel = {System.Text.Json.JsonSerializer.Serialize(ViewModel.SelectedParameter)};");
            await ChartWebView.EvaluateJavaScriptAsync($"window.dotnetData={json};");
            await ChartWebView.EvaluateJavaScriptAsync("renderChart();");
        }
    }
}
