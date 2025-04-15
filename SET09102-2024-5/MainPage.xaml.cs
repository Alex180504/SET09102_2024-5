using SET09102_2024_5.ViewModels;

namespace SET09102_2024_5
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainPageViewModel();
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainPageViewModel viewModel)
            {
                viewModel.Count++;
            }
        }
    }
}
