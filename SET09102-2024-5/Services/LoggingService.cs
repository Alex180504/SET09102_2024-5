using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace SET09102_2024_5.Services
{
    /// <summary>
    /// Implementation of the logging service
    /// </summary>
    public class LoggingService : ILoggingService, IBaseService
    {
        private const string LogFileName = "app_log.txt";
        private readonly List<string> _memoryLogs = new List<string>();
        private readonly object _logLock = new object();
        private bool _isInitialized = false;
        
        /// <summary>
        /// Initialize the logging service
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;
                
            try
            {
                // Create a timestamped entry when logging starts
                string startupMessage = $"=== Log Started: {DateTime.Now} ===";
                _memoryLogs.Add(startupMessage);
                
                // Flush any cached logs if needed
                await FlushLogsAsync();
                
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                // Use system diagnostics as fallback
                System.Diagnostics.Debug.WriteLine($"Failed to initialize logging: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Clean up resources and flush remaining logs
        /// </summary>
        public async Task CleanupAsync()
        {
            await FlushLogsAsync();
        }

        /// <summary>
        /// Log debug information
        /// </summary>
        public void Debug(string message, string category = "General")
        {
            LogMessage("DEBUG", message, category);
        }

        /// <summary>
        /// Log information
        /// </summary>
        public void Info(string message, string category = "General")
        {
            LogMessage("INFO", message, category);
        }

        /// <summary>
        /// Log warning information
        /// </summary>
        public void Warning(string message, string category = "General")
        {
            LogMessage("WARNING", message, category);
        }

        /// <summary>
        /// Log error information
        /// </summary>
        public void Error(string message, Exception exception = null, string category = "General")
        {
            string logMessage = message;
            
            if (exception != null)
            {
                logMessage += $" | Exception: {exception.GetType().Name} | {exception.Message}";
                if (exception.StackTrace != null)
                {
                    logMessage += $" | Stack: {exception.StackTrace}";
                }
            }
            
            LogMessage("ERROR", logMessage, category);
        }
        
        /// <summary>
        /// Core logging method that handles all log types
        /// </summary>
        private void LogMessage(string level, string message, string category)
        {
            // Create formatted log entry
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logEntry = $"[{timestamp}] {level} [{category}] {message}";
            
            // Always output to debug console
            System.Diagnostics.Debug.WriteLine(logEntry);
            
            // Store in memory buffer
            lock (_logLock)
            {
                _memoryLogs.Add(logEntry);
                
                // If buffer gets too large, trigger async flush
                if (_memoryLogs.Count > 100)
                {
                    _ = FlushLogsAsync();
                }
            }
        }
        
        /// <summary>
        /// Write logs to persistent storage
        /// </summary>
        private async Task FlushLogsAsync()
        {
            if (_memoryLogs.Count == 0)
                return;
                
            try
            {
                List<string> logsToWrite;
                
                // Get a copy of the current logs and clear buffer
                lock (_logLock)
                {
                    logsToWrite = new List<string>(_memoryLogs);
                    _memoryLogs.Clear();
                }
                
                // Get the app data directory
                string logDirectory = FileSystem.AppDataDirectory;
                string logFilePath = Path.Combine(logDirectory, LogFileName);
                
                // Append logs to file
                await File.AppendAllLinesAsync(logFilePath, logsToWrite);
            }
            catch (Exception ex)
            {
                // Use system diagnostics as fallback
                System.Diagnostics.Debug.WriteLine($"Failed to write logs: {ex.Message}");
            }
        }
    }
}