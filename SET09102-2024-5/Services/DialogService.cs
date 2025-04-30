// Services/DialogService.cs
using SET09102_2024_5.Interfaces;

namespace SET09102_2024_5.Services
{
    /// <summary>
    /// MAUI implementation of IDialogService that provides standardized dialog and alert
    /// functionality across the application. This service encapsulates the platform-specific
    /// dialog implementation provided by .NET MAUI's Application.Current.MainPage.
    /// </summary>
    public class DialogService : IDialogService
    {
        /// <summary>
        /// Displays an alert dialog with a single cancellation button.
        /// </summary>
        /// <param name="title">The title of the alert dialog that appears at the top.</param>
        /// <param name="message">The message content to display in the dialog body.</param>
        /// <param name="cancel">The text for the cancellation button (e.g., "OK", "Cancel", "Close").</param>
        /// <returns>A task that represents the asynchronous dialog operation.</returns>
        /// <remarks>
        /// This method directly uses MAUI's DisplayAlert method. The dialog will block the UI thread
        /// until the user dismisses it by tapping the cancel button.
        /// </remarks>
        public Task DisplayAlertAsync(string title, string message, string cancel)
        {
            return Application.Current.MainPage.DisplayAlert(title, message, cancel);
        }

        /// <summary>
        /// Displays a confirmation dialog with both accept and cancel buttons.
        /// </summary>
        /// <param name="title">The title of the confirmation dialog that appears at the top.</param>
        /// <param name="message">The message content to display in the dialog body.</param>
        /// <param name="accept">The text for the accept/confirmation button (e.g., "Yes", "OK", "Accept").</param>
        /// <param name="cancel">The text for the cancellation button (e.g., "No", "Cancel").</param>
        /// <returns>
        /// A task that represents the asynchronous dialog operation. 
        /// The task result contains true if the user accepts; false if the user cancels.
        /// </returns>
        /// <remarks>
        /// This method directly uses MAUI's DisplayAlert method with accept/cancel options. The dialog
        /// will block the UI thread until the user makes a selection. The accept button is displayed
        /// first, followed by the cancel button.
        /// </remarks>
        public Task<bool> DisplayConfirmationAsync(string title, string message, string accept, string cancel)
        {
            return Application.Current.MainPage.DisplayAlert(title, message, accept, cancel);
        }

        /// <summary>
        /// Displays an error dialog with a standardized appearance and a single OK button.
        /// </summary>
        /// <param name="message">The error message content to display in the dialog body.</param>
        /// <param name="title">The title of the error dialog that appears at the top. Defaults to "Error".</param>
        /// <returns>A task that represents the asynchronous dialog operation.</returns>
        /// <remarks>
        /// This method uses a standardized format for error messages with a consistent OK button.
        /// Use this method for displaying error notifications to maintain a consistent user experience.
        /// </remarks>
        public Task DisplayErrorAsync(string message, string title = "Error")
        {
            return Application.Current.MainPage.DisplayAlert(title, message, "OK");
        }

        /// <summary>
        /// Displays a success dialog with a standardized appearance and a single OK button.
        /// </summary>
        /// <param name="message">The success message content to display in the dialog body.</param>
        /// <param name="title">The title of the success dialog that appears at the top. Defaults to "Success".</param>
        /// <returns>A task that represents the asynchronous dialog operation.</returns>
        /// <remarks>
        /// This method uses a standardized format for success messages with a consistent OK button.
        /// Use this method for displaying success notifications to maintain a consistent user experience.
        /// </remarks>
        public Task DisplaySuccessAsync(string message, string title = "Success")
        {
            return Application.Current.MainPage.DisplayAlert(title, message, "OK");
        }
    }
}
