using SET09102_2024_5.ViewModels;
using System;

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
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Explicitly load data when the page appears
        await _viewModel.LoadDataAsync();
    }
    
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Clean up any resources or event handlers
        _viewModel?.Cleanup();
    }
    
    // Animation helper for item selection
    private async void OnItemSelected(object sender, EventArgs e)
    {
        if (sender is View view)
        {
            // Simple animation for selection feedback
            await view.ScaleTo(0.95, 100);
            await view.ScaleTo(1.0, 100);
        }
    }
}