using System;

namespace SET09102_2024_5.Interfaces
{
    /// <summary>
    /// Service for centralized logging across the application
    /// </summary>
    public interface ILoggingService : IBaseService
    {
        /// <summary>
        /// Log debug information
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="category">Optional category for filtering logs</param>
        void Debug(string message, string category = "General");
        
        /// <summary>
        /// Log information
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="category">Optional category for filtering logs</param>
        void Info(string message, string category = "General");
        
        /// <summary>
        /// Log warning information
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="category">Optional category for filtering logs</param>
        void Warning(string message, string category = "General");
        
        /// <summary>
        /// Log error information
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="exception">Optional exception that caused the error</param>
        /// <param name="category">Optional category for filtering logs</param>
        void Error(string message, Exception exception = null, string category = "General");
    }
}