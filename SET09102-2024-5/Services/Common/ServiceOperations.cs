using System;
using System.Threading;
using System.Threading.Tasks;
using SET09102_2024_5.Interfaces;

namespace SET09102_2024_5.Services.Common
{
    /// <summary>
    /// Helper class to standardize service operation error handling and logging.
    /// </summary>
    public static class ServiceOperations
    {
        /// <summary>
        /// Executes a service operation with standardized error handling and logging.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="logger">The logging service to use for logging.</param>
        /// <param name="category">The category for logging.</param>
        /// <param name="operationName">The name of the operation for logging.</param>
        /// <param name="defaultValue">The default value to return if the operation fails.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the operation result or default value if the operation fails.</returns>
        public static async Task<(bool Success, T Value, Exception Error)> ExecuteAsync<T>(
            Func<Task<T>> operation,
            ILoggingService logger,
            string category,
            string operationName,
            T defaultValue = default,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (logger != null)
                {
                    logger.Debug($"Starting operation: {operationName}", category);
                }

                // Check for cancellation before starting
                cancellationToken.ThrowIfCancellationRequested();

                var result = await operation();
                
                if (logger != null)
                {
                    logger.Debug($"Operation completed successfully: {operationName}", category);
                }

                return (true, result, null);
            }
            catch (OperationCanceledException ex)
            {
                if (logger != null)
                {
                    logger.Warning($"Operation cancelled: {operationName}", category);
                }
                return (false, defaultValue, ex);
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.Error($"Operation failed: {operationName}", ex, category);
                }
                return (false, defaultValue, ex);
            }
        }

        /// <summary>
        /// Executes a void service operation with standardized error handling and logging.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="logger">The logging service to use for logging.</param>
        /// <param name="category">The category for logging.</param>
        /// <param name="operationName">The name of the operation for logging.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the operation completion with success flag and any error.</returns>
        public static async Task<(bool Success, Exception Error)> ExecuteAsync(
            Func<Task> operation,
            ILoggingService logger,
            string category,
            string operationName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (logger != null)
                {
                    logger.Debug($"Starting operation: {operationName}", category);
                }

                // Check for cancellation before starting
                cancellationToken.ThrowIfCancellationRequested();

                await operation();
                
                if (logger != null)
                {
                    logger.Debug($"Operation completed successfully: {operationName}", category);
                }

                return (true, null);
            }
            catch (OperationCanceledException ex)
            {
                if (logger != null)
                {
                    logger.Warning($"Operation cancelled: {operationName}", category);
                }
                return (false, ex);
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.Error($"Operation failed: {operationName}", ex, category);
                }
                return (false, ex);
            }
        }

        /// <summary>
        /// Executes a service operation with retries on failure.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="logger">The logging service to use for logging.</param>
        /// <param name="category">The category for logging.</param>
        /// <param name="operationName">The name of the operation for logging.</param>
        /// <param name="retryCount">The number of retries to attempt.</param>
        /// <param name="retryDelayMs">The delay in milliseconds between retries.</param>
        /// <param name="defaultValue">The default value to return if all retries fail.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task representing the operation result or default value if all retries fail.</returns>
        public static async Task<(bool Success, T Value, Exception Error)> ExecuteWithRetryAsync<T>(
            Func<Task<T>> operation,
            ILoggingService logger,
            string category,
            string operationName,
            int retryCount = 3,
            int retryDelayMs = 1000,
            T defaultValue = default,
            CancellationToken cancellationToken = default)
        {
            Exception lastException = null;
            
            for (int attempt = 0; attempt <= retryCount; attempt++)
            {
                try
                {
                    // Check for cancellation before starting each attempt
                    cancellationToken.ThrowIfCancellationRequested();

                    if (attempt > 0 && logger != null)
                    {
                        logger.Info($"Retry attempt {attempt} of {retryCount} for operation: {operationName}", category);
                    }

                    var result = await operation();
                    
                    if (attempt > 0 && logger != null)
                    {
                        logger.Info($"Operation succeeded on retry attempt {attempt}: {operationName}", category);
                    }
                    
                    return (true, result, null);
                }
                catch (OperationCanceledException ex)
                {
                    if (logger != null)
                    {
                        logger.Warning($"Operation cancelled: {operationName}", category);
                    }
                    return (false, defaultValue, ex);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    
                    if (logger != null)
                    {
                        if (attempt < retryCount)
                        {
                            logger.Warning($"Operation failed (attempt {attempt + 1} of {retryCount + 1}): {operationName}. Error: {ex.Message}", category);
                            
                            // Wait before retrying, with exponential backoff
                            await Task.Delay(retryDelayMs * (int)Math.Pow(2, attempt), cancellationToken);
                        }
                        else
                        {
                            logger.Error($"All retry attempts failed for operation: {operationName}", ex, category);
                        }
                    }
                }
            }
            
            return (false, defaultValue, lastException);
        }

        /// <summary>
        /// Executes an operation with a timeout.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="timeout">The timeout duration.</param>
        /// <param name="logger">The logging service.</param>
        /// <param name="category">The logging category.</param>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="defaultValue">Default value to return on timeout.</param>
        /// <returns>A task representing the operation result or default value if timeout occurs.</returns>
        public static async Task<(bool Success, T Value, Exception Error)> ExecuteWithTimeoutAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            TimeSpan timeout,
            ILoggingService logger,
            string category,
            string operationName,
            T defaultValue = default)
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(timeout);
            
            try
            {
                if (logger != null)
                {
                    logger.Debug($"Starting operation with {timeout.TotalSeconds}s timeout: {operationName}", category);
                }
                
                var result = await operation(cts.Token);
                
                if (logger != null)
                {
                    logger.Debug($"Operation completed within timeout: {operationName}", category);
                }
                
                return (true, result, null);
            }
            catch (OperationCanceledException ex) when (cts.IsCancellationRequested)
            {
                if (logger != null)
                {
                    logger.Warning($"Operation timed out after {timeout.TotalSeconds}s: {operationName}", category);
                }
                return (false, defaultValue, new TimeoutException($"Operation timed out: {operationName}", ex));
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.Error($"Operation failed: {operationName}", ex, category);
                }
                return (false, defaultValue, ex);
            }
        }
    }
}