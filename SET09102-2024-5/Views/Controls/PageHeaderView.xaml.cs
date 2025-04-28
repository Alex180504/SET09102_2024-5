using System.Windows.Input;
using Microsoft.Maui.Graphics;

namespace SET09102_2024_5.Views.Controls
{
    public partial class PageHeaderView : ContentView
    {
        public static readonly BindableProperty IconTextProperty = 
            BindableProperty.Create(nameof(IconText), typeof(string), typeof(PageHeaderView), string.Empty);
            
        public static readonly BindableProperty TitleProperty = 
            BindableProperty.Create(nameof(Title), typeof(string), typeof(PageHeaderView), string.Empty);
            
        public static readonly BindableProperty SubtitleProperty = 
            BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(PageHeaderView), string.Empty);
            
        public static readonly BindableProperty BackgroundColorProperty = 
            BindableProperty.Create(nameof(BackgroundColor), typeof(Color), typeof(PageHeaderView), Colors.Blue);
            
        public static readonly BindableProperty ActionIconTextProperty = 
            BindableProperty.Create(nameof(ActionIconText), typeof(string), typeof(PageHeaderView), string.Empty);
            
        public static readonly BindableProperty ActionCommandProperty = 
            BindableProperty.Create(nameof(ActionCommand), typeof(ICommand), typeof(PageHeaderView), null);
        
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
        
        public Color BackgroundColor
        {
            get => (Color)GetValue(BackgroundColorProperty);
            set => SetValue(BackgroundColorProperty, value);
        }
        
        public string ActionIconText
        {
            get => (string)GetValue(ActionIconTextProperty);
            set => SetValue(ActionIconTextProperty, value);
        }
        
        public ICommand ActionCommand
        {
            get => (ICommand)GetValue(ActionCommandProperty);
            set => SetValue(ActionCommandProperty, value);
        }
        
        public PageHeaderView()
        {
            InitializeComponent();
        }
    }
}