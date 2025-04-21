// Interfaces/INavigationService.cs
namespace SET09102_2024_5.Interfaces
{
    /// <summary>
    /// Provides navigation functionality between pages, abstracting MAUI's navigation
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Navigates to the specified page
        /// </summary>
        Task NavigateToAsync<T>(object parameter = null) where T : Page;

        /// <summary>
        /// Navigates back to the previous page
        /// </summary>
        Task NavigateBackAsync();
    }
}

