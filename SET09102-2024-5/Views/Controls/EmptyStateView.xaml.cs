using System.Windows.Input;

namespace SET09102_2024_5.Views.Controls
{
    public partial class EmptyStateView : ContentView
    {
        public static readonly BindableProperty IconTextProperty = 
            BindableProperty.Create(nameof(IconText), typeof(string), typeof(EmptyStateView), string.Empty);
            
        public static readonly BindableProperty TitleProperty = 
            BindableProperty.Create(nameof(Title), typeof(string), typeof(EmptyStateView), "No Items Found");
            
        public static readonly BindableProperty SubtitleProperty = 
            BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(EmptyStateView), string.Empty);
            
        public static readonly BindableProperty ActionTextProperty = 
            BindableProperty.Create(nameof(ActionText), typeof(string), typeof(EmptyStateView), string.Empty);
            
        public static readonly BindableProperty ActionCommandProperty = 
            BindableProperty.Create(nameof(ActionCommand), typeof(ICommand), typeof(EmptyStateView), null);
        
        public string IconText
        {
            get => (string)GetValue(IconTextProperty);
            set => SetValue(IconTextProperty, value);
        }
        
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        
        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }
        
        public string ActionText
        {
            get => (string)GetValue(ActionTextProperty);
            set => SetValue(ActionTextProperty, value);
        }
        
        public ICommand ActionCommand
        {
            get => (ICommand)GetValue(ActionCommandProperty);
            set => SetValue(ActionCommandProperty, value);
        }
        
        public EmptyStateView()
        {
            InitializeComponent();
        }
    }
}