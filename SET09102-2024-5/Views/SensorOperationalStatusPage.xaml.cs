using SET09102_2024_5.ViewModels;

namespace SET09102_2024_5.Views
{
    public partial class SensorOperationalStatusPage : ContentPage
    {
        private readonly SensorOperationalStatusViewModel _viewModel;

        public SensorOperationalStatusPage(SensorOperationalStatusViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.LoadSensorsCommand.Execute(null);
        }
        

    }
}
