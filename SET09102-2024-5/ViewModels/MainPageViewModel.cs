using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SET09102_2024_5.ViewModels
{
    public partial class MainPageViewModel : BaseViewModel
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CounterText))]
        private int _count;

        public string CounterText => Count == 1 ? $"Clicked {Count} time" : $"Clicked {Count} times";

        public MainPageViewModel()
        {
            Title = "Main Page";
        }

        [RelayCommand]
        private void IncrementCount()
        {
            Count++;
        }
    }
}