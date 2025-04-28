using SET09102_2024_5.Interfaces;

namespace SET09102_2024_5.Tests.Mocks
{
    public class MockDialogService : IDialogService
    {
        // Store dialog interactions for verification in tests
        public List<string> DisplayedMessages { get; } = new List<string>();
        public List<string> DisplayedTitles { get; } = new List<string>();

        // Configure response for confirmation dialogs
        public bool ConfirmationResponse { get; set; } = true;

        public Task DisplayAlertAsync(string title, string message, string cancel)
        {
            DisplayedTitles.Add(title);
            DisplayedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task<bool> DisplayConfirmationAsync(string title, string message, string accept, string cancel)
        {
            DisplayedTitles.Add(title);
            DisplayedMessages.Add(message);
            return Task.FromResult(ConfirmationResponse);
        }

        public Task DisplayErrorAsync(string message, string title = "Error")
        {
            DisplayedTitles.Add(title);
            DisplayedMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task DisplaySuccessAsync(string message, string title = "Success")
        {
            DisplayedTitles.Add(title);
            DisplayedMessages.Add(message);
            return Task.CompletedTask;
        }
    }
}

