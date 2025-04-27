using SET09102_2024_5.ViewModels;


namespace SET09102_2024_5.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly MapPage _mapPage;

        public MainPage(MainPageViewModel vm, MapPage mapPage)
        {
            InitializeComponent();
            BindingContext = vm;
            _mapPage = mapPage;
        }
    }
}
