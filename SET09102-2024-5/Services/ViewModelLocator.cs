using System;
using Microsoft.Extensions.DependencyInjection;
using SET09102_2024_5.ViewModels;

namespace SET09102_2024_5.Services
{
    /// <summary>
    /// Service that manages view model resolution and caching for improved performance and memory usage
    /// </summary>
    public class ViewModelLocator
    {
        private readonly IServiceProvider _serviceProvider;

        public ViewModelLocator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Gets the viewmodel for a view, with optional caching strategy
        /// </summary>
        /// <typeparam name="TViewModel">Type of viewmodel to resolve</typeparam>
        /// <param name="cacheViewModel">Whether to cache the viewmodel instance</param>
        /// <returns>The viewmodel instance</returns>
        public TViewModel GetViewModel<TViewModel>(bool cacheViewModel = false) where TViewModel : BaseViewModel
        {
            // For cached viewmodels, use GetRequiredService (singleton)
            // For non-cached, use CreateScope to get a transient instance
            if (cacheViewModel)
            {
                return _serviceProvider.GetRequiredService<TViewModel>();
            }
            else
            {
                using var scope = _serviceProvider.CreateScope();
                return scope.ServiceProvider.GetRequiredService<TViewModel>();
            }
        }

        /// <summary>
        /// Releases any cached viewmodels that may be holding resources
        /// </summary>
        public void CleanupCache()
        {
            // This can be expanded to handle cleanup of any cached viewmodels
            // For now it's a placeholder for future implementation
        }
    }
}