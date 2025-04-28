using System;
using System.Threading.Tasks;
using SET09102_2024_5.Interfaces;

namespace SET09102_2024_5.Services
{
    /// <summary>
    /// Helper class to standardize service operations and error handling
    /// </summary>
    public static class ServiceOperations
    {
        /// <summary>
        /// Execute an operation with standardized error handling and logging
        /// </summary>
        public static async Task<ServiceResult<T>> ExecuteAsync<T>(
            Func<Task<T>> operation,
            ILoggingService logger,
            string category,
            string operationName,
            T defaultValue = default)
        {
            try
            {
                logger.Debug($"Executing operation: {operationName}", category);
                var result = await operation();
                logger.Debug($"Operation completed successfully: {operationName}", category);
                return ServiceResult<T>.CreateSuccess(result);
            }
            catch (Exception ex)
            {
                logger.Error($"Error executing {operationName}", ex, category);
                return ServiceResult<T>.CreateError($"Error executing {operationName}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Execute an operation without a return value with standardized error handling and logging
        /// </summary>
        public static async Task<ServiceResult> ExecuteAsync(
            Func<Task> operation,
            ILoggingService logger,
            string category,
            string operationName)
        {
            try
            {
                logger.Debug($"Executing operation: {operationName}", category);
                await operation();
                logger.Debug($"Operation completed successfully: {operationName}", category);
                return ServiceResult.CreateSuccess();
            }
            catch (Exception ex)
            {
                logger.Error($"Error executing {operationName}", ex, category);
                return ServiceResult.CreateError($"Error executing {operationName}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Execute a validation operation with standardized error handling and logging
        /// </summary>
        public static ServiceResult Validate(
            Func<bool> validation,
            string errorMessage,
            ILoggingService logger,
            string category,
            string operationName)
        {
            try
            {
                logger.Debug($"Validating: {operationName}", category);
                if (validation())
                {
                    logger.Debug($"Validation passed: {operationName}", category);
                    return ServiceResult.CreateSuccess();
                }
                else
                {
                    logger.Warning($"Validation failed: {operationName} - {errorMessage}", category);
                    return ServiceResult.CreateError(errorMessage);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error during validation {operationName}", ex, category);
                return ServiceResult.CreateError($"Validation error: {ex.Message}", ex);
            }
        }
    }
}