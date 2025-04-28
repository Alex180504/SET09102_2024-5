using SET09102_2024_5.ViewModels;

namespace SET09102_2024_5.Views;

public partial class MaintenanceDetailPage : ContentPage
{
    public MaintenanceDetailPage(MaintenanceDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}