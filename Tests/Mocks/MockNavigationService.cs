using SET09102_2024_5.Interfaces;

namespace SET09102_2024_5.Tests.Mocks
{
    public class MockNavigationService : INavigationService
    {
        // Store navigation history for verification in tests
        public List<Type> NavigationHistory { get; } = new List<Type>();
        public List<object> NavigationParameters { get; } = new List<object>();
        public int BackNavigationCount { get; private set; } = 0;

        public Task NavigateToAsync<T>(object parameter = null) where T : Page
        {
            NavigationHistory.Add(typeof(T));
            NavigationParameters.Add(parameter);
            return Task.CompletedTask;
        }

        public Task NavigateBackAsync()
        {
            BackNavigationCount++;
            return Task.CompletedTask;
        }
    }
}

