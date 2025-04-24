using CommunityToolkit.Mvvm.ComponentModel;

namespace SET09102_2024_5.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        // Utility method for derived view models to use during async operations
        protected void StartBusy(string operationTitle = null)
        {
            IsBusy = true;
            if (!string.IsNullOrEmpty(operationTitle))
                Title = operationTitle;
        }

        // Utility method to reset busy state
        protected void EndBusy(string resetTitle = null)
        {
            IsBusy = false;
            if (!string.IsNullOrEmpty(resetTitle))
                Title = resetTitle;
        }
    }
}