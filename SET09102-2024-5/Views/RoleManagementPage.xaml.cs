using SET09102_2024_5.ViewModels;

namespace SET09102_2024_5.Views;

public partial class RoleManagementPage : ContentPage
{
    private readonly RoleManagementViewModel _viewModel;
    
    public RoleManagementPage(RoleManagementViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
    
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Additional initialization if needed when page appears
    }
}