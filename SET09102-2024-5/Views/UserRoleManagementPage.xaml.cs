using SET09102_2024_5.ViewModels;
using System.Threading.Tasks;

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
        
        // Apply entrance animations
        this.Opacity = 0;
        await this.FadeTo(1, 250, Easing.CubicOut);
        
        // Additional initialization if needed when page appears
    }
    
    // Animation helper for item selection
    private async Task AnimateItemSelection(View item)
    {
        if (item == null) return;
        
        // Quick scale animation to provide visual feedback
        await item.ScaleTo(0.97, 100, Easing.CubicOut);
        await Task.Delay(50);
        await item.ScaleTo(1, 100, Easing.SpringOut);
    }
}