using SET09102_2024_5.Views;

namespace SET09102_2024_5
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register routes for navigation
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
            Routing.RegisterRoute(nameof(SensorManagementPage), typeof(SensorManagementPage));
            
            Routing.RegisterRoute(nameof(Features.HistoricalData.HistoricalDataPage), typeof(Features.HistoricalData.HistoricalDataPage));
            Routing.RegisterRoute(nameof(MapPage), typeof(MapPage));
        }
    }
}
