using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SET09102_2024_5.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private int count;

        public int Count
        {
            get => count;
            set
            {
                count = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CounterText));
            }
        }

        public string CounterText => Count == 1 ? $"Clicked {Count} time" : $"Clicked {Count} times";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}