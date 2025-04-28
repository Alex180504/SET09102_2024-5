using SET09102_2024_5.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace SET09102_2024_5.Views
{
    public partial class SensorOperationalStatusPage : ViewBase
    {
        private SensorOperationalStatusViewModel _viewModel => BindingContext as SensorOperationalStatusViewModel;

        // Add parameterless constructor for Shell navigation
        public SensorOperationalStatusPage()
        {
            InitializeComponent();
            BindingContext = App.Current.Handler.MauiContext?.Services.GetService<SensorOperationalStatusViewModel>();
        }

        public SensorOperationalStatusPage(SensorOperationalStatusViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_viewModel != null)
            {
                _viewModel.LoadSensorsCommand.Execute(null);
            }
        }
    }
}
