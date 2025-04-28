using SET09102_2024_5.Interfaces;

namespace SET09102_2024_5.Tests.Mocks
{
    public class MockMainThreadService : IMainThreadService
    {
        public bool IsMainThread => true;

        public void BeginInvokeOnMainThread(Action action)
        {
            // Execute immediately on the current thread
            action?.Invoke();
        }

        public Task InvokeOnMainThreadAsync(Action action)
        {
            action?.Invoke();
            return Task.CompletedTask;
        }

        public Task<T> InvokeOnMainThreadAsync<T>(Func<T> function)
        {
            return Task.FromResult(function());
        }
    }
}

