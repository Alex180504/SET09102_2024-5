using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace SET09102_2024_5.Views
{
    // Note: This class is intentionally NOT partial to allow derived classes to implement partial
    public abstract class ViewBase : ContentPage, IDisposable
    {
        private bool _disposed = false;
        
        /// <summary>
        /// Animates a control to provide visual feedback for selection
        /// </summary>
        protected async Task AnimateControlSelection(View control)
        {
            if (control == null) return;
            
            // Scale down and up with a slight bounce effect
            await control.ScaleTo(0.95, 100);
            await control.ScaleTo(1.05, 100);
            await control.ScaleTo(1.0, 100);
        }
        
        /// <summary>
        /// Virtual method called when the view is disappearing
        /// </summary>
        protected virtual void OnViewDisappearing(object sender, EventArgs e)
        {
            // Base implementation does nothing
        }
        
        /// <summary>
        /// Dispose of resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Dispose pattern implementation
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    // Unsubscribe from events, etc.
                }
                
                // Dispose unmanaged resources
                
                _disposed = true;
            }
        }
    }
}