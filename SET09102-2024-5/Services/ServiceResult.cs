using System;
using System.Collections.Generic;

namespace SET09102_2024_5.Services
{
    /// <summary>
    /// Generic result pattern for standardized service method responses
    /// </summary>
    public class ServiceResult<T>
    {
        /// <summary>
        /// The resulting value if the operation was successful
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// Flag indicating if the operation was successful
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// Error message if the operation failed
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Exception if one was caught
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Create a successful result with a value
        /// </summary>
        public static ServiceResult<T> CreateSuccess(T value)
        {
            return new ServiceResult<T>
            {
                Value = value,
                Success = true
            };
        }

        /// <summary>
        /// Create a failure result with an error message
        /// </summary>
        public static ServiceResult<T> CreateError(string message)
        {
            return new ServiceResult<T>
            {
                Success = false,
                ErrorMessage = message
            };
        }

        /// <summary>
        /// Create a failure result from an exception
        /// </summary>
        public static ServiceResult<T> CreateError(string message, Exception ex)
        {
            return new ServiceResult<T>
            {
                Success = false,
                ErrorMessage = message,
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Non-generic result for operations that don't return a value
    /// </summary>
    public class ServiceResult
    {
        /// <summary>
        /// Flag indicating if the operation was successful
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// Error message if the operation failed
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Exception if one was caught
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Create a successful result
        /// </summary>
        public static ServiceResult CreateSuccess()
        {
            return new ServiceResult
            {
                Success = true
            };
        }

        /// <summary>
        /// Create a failure result with an error message
        /// </summary>
        public static ServiceResult CreateError(string message)
        {
            return new ServiceResult
            {
                Success = false,
                ErrorMessage = message
            };
        }

        /// <summary>
        /// Create a failure result from an exception
        /// </summary>
        public static ServiceResult CreateError(string message, Exception ex)
        {
            return new ServiceResult
            {
                Success = false,
                ErrorMessage = message,
                Exception = ex
            };
        }
    }
}