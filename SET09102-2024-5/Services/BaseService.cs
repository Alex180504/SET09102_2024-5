using System;
using System.Threading.Tasks;
using SET09102_2024_5.Interfaces;

namespace SET09102_2024_5.Services
{
    /// <summary>
    /// Base implementation of IBaseService to eliminate duplicate initialization code
    /// </summary>
    public abstract class BaseService : IBaseService
    {
        protected readonly ILoggingService _loggingService;
        protected bool _isInitialized = false;
        protected readonly string _serviceName;
        protected readonly string _serviceCategory;

        protected BaseService(string serviceName, string serviceCategory, ILoggingService loggingService)
        {
            _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
            _serviceCategory = serviceCategory ?? throw new ArgumentNullException(nameof(serviceCategory));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

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

        protected abstract Task InitializeInternalAsync();

        public virtual Task<bool> IsReadyAsync()
        {
            return Task.FromResult(_isInitialized);
        }

        public virtual string GetServiceStatus()
        {
            return _isInitialized ? "Ready" : "Not Ready";
        }

        public virtual string GetServiceName()
        {
            return _serviceName;
        }

        public virtual async Task CleanupAsync()
        {
            _loggingService.Info($"Cleaning up {_serviceName}", _serviceCategory);
            await Task.CompletedTask;
        }
    }
}