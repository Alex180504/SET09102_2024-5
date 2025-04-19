using Microsoft.Extensions.DependencyInjection;
using SET09102_2024_5.Services;

namespace SET09102_2024_5
{
    public partial class App : Application
    {
        public App(IServiceProvider services)
        {
            InitializeComponent();

            // Get IAuthService directly from the service provider
            var authService = services.GetRequiredService<IAuthService>();
            
            // Create AppShell with the authService
            MainPage = new AppShell(authService);
        }
        
        // Parameterless constructor for design-time or other uses
        public App()
        {
            InitializeComponent();
            
            // This will only be called in design time or when services aren't available
            // In real runtime, the constructor with IServiceProvider will be used
        }
    }
}
