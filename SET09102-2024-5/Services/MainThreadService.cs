// Services/MainThreadService.cs
using SET09102_2024_5.Interfaces;

namespace SET09102_2024_5.Services
{
    /// <summary>
    /// MAUI implementation of IMainThreadService
    /// </summary>
    public class MainThreadService : IMainThreadService
    {
        public bool IsMainThread => MainThread.IsMainThread;

        public void BeginInvokeOnMainThread(Action action)
        {
            MainThread.BeginInvokeOnMainThread(action);
        }

        public Task InvokeOnMainThreadAsync(Action action)
        {
            return MainThread.InvokeOnMainThreadAsync(action);
        }

        public Task<T> InvokeOnMainThreadAsync<T>(Func<T> function)
        {
            return MainThread.InvokeOnMainThreadAsync(function);
        }
    }
}

