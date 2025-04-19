using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SET09102_2024_5.ViewModels
{
    public class BaseViewModel : ObservableObject
    {
        private string _title = string.Empty;
        
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
    }
}