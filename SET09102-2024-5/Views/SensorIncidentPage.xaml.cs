using SET09102_2024_5.ViewModels;

namespace SET09102_2024_5.Views
{
    public partial class SensorIncidentPage : ContentPage
    {
        private readonly SensorIncidentLogViewModel _viewModel;

        public SensorIncidentPage(SensorIncidentLogViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.LoadIncidentsCommand.Execute(null);
        }
    }
}
