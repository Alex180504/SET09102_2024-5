using SET09102_2024_5.ViewModels;

namespace SET09102_2024_5.Views;

public partial class UserRoleManagementPage : ContentPage
{
    private readonly UserRoleManagementViewModel _viewModel;
    
    public UserRoleManagementPage(UserRoleManagementViewModel viewModel)
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