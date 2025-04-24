using SET09102_2024_5.ViewModels;
using System.Threading.Tasks;

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
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Apply entrance animations
        this.Opacity = 0;
        await this.FadeTo(1, 250, Easing.CubicOut);
        
        // Additional initialization if needed when page appears
    }

    // Animation helper for card selection
    public async Task AnimateCardSelection(View card)
    {
        if (card == null) return;
        
        // Quick scale animation to provide visual feedback
        await card.ScaleTo(0.97, 100, Easing.CubicOut);
        await Task.Delay(50);
        await card.ScaleTo(1, 100, Easing.SpringOut);
    }
}