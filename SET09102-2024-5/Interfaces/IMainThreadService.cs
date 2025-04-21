namespace SET09102_2024_5.Interfaces
{
    /// <summary>
    /// Provides thread-related operations, abstracting MAUI's MainThread functionality
    /// </summary>
    public interface IMainThreadService
    {
        /// <summary>
        /// Determines whether the current thread is the main UI thread
        /// </summary>
        bool IsMainThread { get; }

        /// <summary>
        /// Executes the given action on the main thread
        /// </summary>
        void BeginInvokeOnMainThread(Action action);

        /// <summary>
        /// Executes the given action on the main thread and returns a task
        /// </summary>
        Task InvokeOnMainThreadAsync(Action action);

        /// <summary>
        /// Executes the given function on the main thread and returns its result
        /// </summary>
        Task<T> InvokeOnMainThreadAsync<T>(Func<T> function);
    }
}

