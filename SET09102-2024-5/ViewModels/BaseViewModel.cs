using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SET09102_2024_5.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasErrors))]
        private string _errorMessage = string.Empty;

        [ObservableProperty] 
        private bool _isRefreshing;

        /// <summary>
        /// Gets whether the ViewModel has any errors
        /// </summary>
        public bool HasErrors => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>
        /// Utility method for derived view models to use during async operations
        /// </summary>
        protected void StartBusy(string operationTitle = null)
        {
            IsBusy = true;
            if (!string.IsNullOrEmpty(operationTitle))
                Title = operationTitle;
            
            System.Diagnostics.Debug.WriteLine($"StartBusy: {operationTitle ?? "No title"}");
        }

        /// <summary>
        /// Utility method to reset busy state
        /// </summary>
        protected void EndBusy(string resetTitle = null)
        {
            if (!string.IsNullOrEmpty(resetTitle))
                Title = resetTitle;
                
            IsBusy = false;
            System.Diagnostics.Debug.WriteLine($"EndBusy: {resetTitle ?? "No title"}");
        }

        /// <summary>
        /// Executes an operation with standard error handling and busy state management
        /// </summary>
        protected async Task ExecuteAsync(Func<Task> operation, string busyMessage = null, string errorPrefix = null, string completedTitle = null)
        {
            if (IsBusy)
            {
                System.Diagnostics.Debug.WriteLine("ExecuteAsync rejected - already busy");
                return;
            }

            try
            {
                ErrorMessage = string.Empty;
                StartBusy(busyMessage);
                await operation();
            }
            catch (Exception ex)
            {
                ErrorMessage = string.IsNullOrEmpty(errorPrefix) 
                    ? ex.Message 
                    : $"{errorPrefix}: {ex.Message}";
                
                System.Diagnostics.Debug.WriteLine($"ExecuteAsync error: {ErrorMessage}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
            finally
            {
                EndBusy(completedTitle);
            }
        }

        /// <summary>
        /// Executes an operation with standard error handling and busy state management, returning a result
        /// </summary>
        protected async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string busyMessage = null, string errorPrefix = null, string completedTitle = null)
        {
            if (IsBusy)
            {
                System.Diagnostics.Debug.WriteLine("ExecuteAsync<T> rejected - already busy");
                return default;
            }

            try
            {
                ErrorMessage = string.Empty;
                StartBusy(busyMessage);
                return await operation();
            }
            catch (Exception ex)
            {
                ErrorMessage = string.IsNullOrEmpty(errorPrefix)
                    ? ex.Message
                    : $"{errorPrefix}: {ex.Message}";
                    
                System.Diagnostics.Debug.WriteLine($"ExecuteAsync<T> error: {ErrorMessage}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return default;
            }
            finally
            {
                EndBusy(completedTitle);
            }
        }

        /// <summary>
        /// Clears any error message
        /// </summary>
        protected void ClearError()
        {
            ErrorMessage = string.Empty;
        }
    }
}