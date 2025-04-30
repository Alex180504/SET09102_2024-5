using System;
using System.Threading.Tasks;
using SET09102_2024_5.Interfaces;

namespace SET09102_2024_5.Services
{
    /// <summary>
    /// Abstract base implementation of IBaseService that provides common service functionality.
    /// This class serves as the foundation for all services in the application, handling
    /// standard initialization, status reporting, and logging. It implements a template method pattern
    /// where derived classes only need to implement specific initialization logic.
    /// </summary>
    public abstract class BaseService : IBaseService
    {
        /// <summary>
        /// Logging service for recording service activities and errors
        /// </summary>
        protected readonly ILoggingService _loggingService;

        /// <summary>
        /// Flag indicating whether the service has been successfully initialized
        /// </summary>
        protected bool _isInitialized = false;

        /// <summary>
        /// The display name of the service used for identification and logging
        /// </summary>
        protected readonly string _serviceName;

        /// <summary>
        /// The category this service belongs to for logging and organization
        /// </summary>
        protected readonly string _serviceCategory;

        /// <summary>
        /// Initializes a new instance of the BaseService class with required dependencies.
        /// </summary>
        /// <param name="serviceName">The name of the service for identification and logging purposes</param>
        /// <param name="serviceCategory">The category this service belongs to for logging and filtering</param>
        /// <param name="loggingService">The logging service for recording service operations</param>
        /// <exception cref="ArgumentNullException">Thrown if any required parameter is null</exception>
        protected BaseService(string serviceName, string serviceCategory, ILoggingService loggingService)
        {
            _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
            _serviceCategory = serviceCategory ?? throw new ArgumentNullException(nameof(serviceCategory));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        /// <summary>
        /// Initializes the service by calling the derived class's initialization logic.
        /// This method handles error logging and state management automatically.
        /// If the service is already initialized, returns immediately with success.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains true if initialization was successful; otherwise, false.
        /// </returns>
        public virtual async Task<bool> InitializeAsync()
        {
            if (_isInitialized)
                return true;

            _loggingService.Info($"Initializing {_serviceName}", _serviceCategory);

            try
            {
                await InitializeInternalAsync();
                _isInitialized = true;
                _loggingService.Info($"{_serviceName} initialized successfully", _serviceCategory);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Failed to initialize {_serviceName}", ex, _serviceCategory);
                return false;
            }
        }

        /// <summary>
        /// Abstract method that must be implemented by derived classes to provide
        /// service-specific initialization logic.
        /// </summary>
        /// <returns>A task representing the asynchronous initialization operation.</returns>
        protected abstract Task InitializeInternalAsync();

        /// <summary>
        /// Checks if the service is ready for use.
        /// A service is considered ready when it has been successfully initialized.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains true if the service is ready; otherwise, false.
        /// </returns>
        public virtual Task<bool> IsReadyAsync()
        {
            return Task.FromResult(_isInitialized);
        }

        /// <summary>
        /// Gets a human-readable status string indicating whether the service is ready for use.
        /// </summary>
        /// <returns>
        /// "Ready" if the service has been initialized successfully; otherwise, "Not Ready".
        /// </returns>
        public virtual string GetServiceStatus()
        {
            return _isInitialized ? "Ready" : "Not Ready";
        }

        /// <summary>
        /// Gets the display name of the service.
        /// </summary>
        /// <returns>The service name as provided during construction.</returns>
        public virtual string GetServiceName()
        {
            return _serviceName;
        }

        /// <summary>
        /// Performs cleanup operations when the service is no longer needed.
        /// Derived classes should override this method to release resources.
        /// </summary>
        /// <returns>A task representing the asynchronous cleanup operation.</returns>
        public virtual async Task CleanupAsync()
        {
            _loggingService.Info($"Cleaning up {_serviceName}", _serviceCategory);
            await Task.CompletedTask;
        }
    }
}
