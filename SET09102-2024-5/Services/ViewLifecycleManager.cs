using System;
using System.Collections.Generic;
using SET09102_2024_5.Views;
using SET09102_2024_5.Interfaces;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace SET09102_2024_5.Services
{
    /// <summary>
    /// Manages the lifecycle of views to improve memory usage and performance
    /// </summary>
    public class ViewLifecycleManager
    {
        // Track active views for memory management
        private readonly Dictionary<string, WeakReference<ViewBase>> _activeViews = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggingService _loggingService;

        public ViewLifecycleManager(IServiceProvider serviceProvider, ILoggingService loggingService)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        /// <summary>
        /// Gets an existing view or creates a new one
        /// </summary>
        /// <typeparam name="TView">The view type to retrieve</typeparam>
        /// <returns>A view instance</returns>
        public TView GetOrCreateView<TView>() where TView : ViewBase
        {
            string viewKey = typeof(TView).FullName ?? typeof(TView).Name;
            
            // Check if the view already exists
            if (_activeViews.TryGetValue(viewKey, out var weakRef))
            {
                // Fix for weak reference target cast
                if (weakRef.TryGetTarget(out var existingView) && existingView is TView typedView)
                {
                    return typedView;
                }
                
                // Remove the expired reference
                _activeViews.Remove(viewKey);
            }
            
            // Create a new view instance using the service provider
            TView newView = _serviceProvider.GetRequiredService<TView>();
            _activeViews[viewKey] = new WeakReference<ViewBase>(newView);
            
            return newView;
        }
        
        /// <summary>
        /// Releases views that are no longer in use
        /// </summary>
        public void CleanupUnusedViews()
        {
            List<string> keysToRemove = new();
            
            foreach (var kvp in _activeViews)
            {
                if (!kvp.Value.TryGetTarget(out _))
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _activeViews.Remove(key);
            }
            
            if (keysToRemove.Any())
            {
                _loggingService.Debug($"Cleaned up {keysToRemove.Count} unused views", "ViewLifecycle");
            }
        }
        
        /// <summary>
        /// Forces disposal of specific view types
        /// </summary>
        /// <typeparam name="TView">The view type to dispose</typeparam>
        public void DisposeView<TView>() where TView : ViewBase
        {
            string viewKey = typeof(TView).FullName ?? typeof(TView).Name;
            
            if (_activeViews.TryGetValue(viewKey, out var weakRef) && 
                weakRef != null && weakRef.TryGetTarget(out ViewBase? view) && view != null)
            {
                view.Dispose();
                _activeViews.Remove(viewKey);
                _loggingService.Debug($"Disposed view: {viewKey}", "ViewLifecycle");
            }
        }
        
        /// <summary>
        /// Gets the count of active views for diagnostics
        /// </summary>
        public int ActiveViewCount => _activeViews.Count;
    }
}