using Microsoft.Maui.Graphics;

namespace SET09102_2024_5.Views.Controls
{
    public partial class LoadingOverlay : ContentView
    {
        public static readonly BindableProperty IsVisibleProperty = 
            BindableProperty.Create(nameof(IsVisible), typeof(bool), typeof(LoadingOverlay), false, 
                propertyChanged: OnIsVisibleChanged);
            
        public static readonly BindableProperty LoadingTextProperty = 
            BindableProperty.Create(nameof(LoadingText), typeof(string), typeof(LoadingOverlay), "Loading...");
            
        public static readonly BindableProperty ActivityIndicatorColorProperty = 
            BindableProperty.Create(nameof(ActivityIndicatorColor), typeof(Color), typeof(LoadingOverlay), null);
            
        public static readonly BindableProperty TextColorProperty = 
            BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(LoadingOverlay), null);
        
        public new bool IsVisible
        {
            get => (bool)GetValue(IsVisibleProperty);
            set => SetValue(IsVisibleProperty, value);
        }
        
        public string LoadingText
        {
            get => (string)GetValue(LoadingTextProperty);
            set => SetValue(LoadingTextProperty, value);
        }
        
        public Color ActivityIndicatorColor
        {
            get => (Color)GetValue(ActivityIndicatorColorProperty);
            set => SetValue(ActivityIndicatorColorProperty, value);
        }
        
        public Color TextColor
        {
            get => (Color)GetValue(TextColorProperty);
            set => SetValue(TextColorProperty, value);
        }
        
        public LoadingOverlay()
        {
            InitializeComponent();
        }

        // Ensure the overlay visibility syncs with the property
        private static void OnIsVisibleChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is LoadingOverlay overlay)
            {
                bool isVisible = (bool)newValue;
                overlay.Opacity = isVisible ? 1 : 0;
                overlay.InputTransparent = !isVisible;
                ((ContentView)overlay).IsVisible = isVisible;
                
                System.Diagnostics.Debug.WriteLine($"LoadingOverlay visibility changed to: {isVisible}");
            }
        }
    }
}