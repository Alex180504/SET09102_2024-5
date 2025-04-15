using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SET09102_2024_5.ViewModels
{
    public class MainPageViewModel : BaseViewModel
    {
        private int count;

        public MainPageViewModel()
        {
            IncrementCountCommand = new Command(OnIncrementCount);
        }

        public int Count
        {
            get => count;
            set
            {
                if (SetProperty(ref count, value))
                {
                    OnPropertyChanged(nameof(CounterText));
                }
            }
        }

        public string CounterText => Count == 1 ? $"Clicked {Count} time" : $"Clicked {Count} times";

        public ICommand IncrementCountCommand { get; }

        private void OnIncrementCount()
        {
            Count++;
        }
    }
}