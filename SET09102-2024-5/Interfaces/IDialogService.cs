// Interfaces/IDialogService.cs
namespace SET09102_2024_5.Interfaces
{
    /// <summary>
    /// Provides methods for displaying dialogs and alerts, abstracting MAUI's UI functionality
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Displays an alert with a single cancellation button
        /// </summary>
        Task DisplayAlertAsync(string title, string message, string cancel);

        /// <summary>
        /// Displays an alert with confirmation and cancellation buttons
        /// </summary>
        Task<bool> DisplayConfirmationAsync(string title, string message, string accept, string cancel);

        /// <summary>
        /// Displays an error alert with the given message
        /// </summary>
        Task DisplayErrorAsync(string message, string title = "Error");

        /// <summary>
        /// Displays a success alert with the given message
        /// </summary>
        Task DisplaySuccessAsync(string message, string title = "Success");
    }
}

