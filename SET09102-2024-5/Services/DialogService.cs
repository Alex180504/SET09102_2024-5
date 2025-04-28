// Services/DialogService.cs
using SET09102_2024_5.Interfaces;

namespace SET09102_2024_5.Services
{
    /// <summary>
    /// MAUI implementation of IDialogService
    /// </summary>
    public class DialogService : IDialogService
    {
        public Task DisplayAlertAsync(string title, string message, string cancel)
        {
            return Application.Current.MainPage.DisplayAlert(title, message, cancel);
        }

        public Task<bool> DisplayConfirmationAsync(string title, string message, string accept, string cancel)
        {
            return Application.Current.MainPage.DisplayAlert(title, message, accept, cancel);
        }

        public Task DisplayErrorAsync(string message, string title = "Error")
        {
            return Application.Current.MainPage.DisplayAlert(title, message, "OK");
        }

        public Task DisplaySuccessAsync(string message, string title = "Success")
        {
            return Application.Current.MainPage.DisplayAlert(title, message, "OK");
        }
    }
}

