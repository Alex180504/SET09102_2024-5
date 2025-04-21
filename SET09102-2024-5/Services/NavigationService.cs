// Services/NavigationService.cs
using SET09102_2024_5.Interfaces;
using System.Reflection;

namespace SET09102_2024_5.Services
{
    /// <summary>
    /// MAUI implementation of INavigationService
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task NavigateToAsync<T>(object parameter = null) where T : Page
        {
            var page = ResolvePage<T>();

            if (page != null)
            {
                // Set the BindingContext's navigation parameter if it has the property
                if (parameter != null && page.BindingContext != null)
                {
                    var property = page.BindingContext.GetType().GetProperty("NavigationParameter",
                        BindingFlags.Public | BindingFlags.Instance);
                    property?.SetValue(page.BindingContext, parameter);
                }

                await Application.Current.MainPage.Navigation.PushAsync(page);
            }
        }

        public Task NavigateBackAsync()
        {
            return Application.Current.MainPage.Navigation.PopAsync();
        }

        private T ResolvePage<T>() where T : Page
        {
            return _serviceProvider.GetService<T>();
        }
    }
}

