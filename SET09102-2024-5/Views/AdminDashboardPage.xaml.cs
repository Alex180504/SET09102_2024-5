using Microsoft.Maui.Controls;
using SET09102_2024_5.Services;
using System;
using System.Threading.Tasks;

namespace SET09102_2024_5.Views;

public partial class AdminDashboardPage : ContentPage
{
    private readonly INavigationService _navigationService;

    public AdminDashboardPage(INavigationService navigationService)
    {
        InitializeComponent();
        _navigationService = navigationService;
    }

    // Apply animations when the page appears
    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            
            // Apply entrance animation
            this.Opacity = 0;
            await this.FadeTo(1, 250, Easing.CubicOut);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Animation error: {ex.Message}");
        }
    }

    private async void OnRoleManagementClicked(object sender, EventArgs e)
    {
        await AnimateButtonClick(sender as Button);
        
        try
        {
            await _navigationService.NavigateToRoleManagementAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Navigation Error", 
                $"Could not navigate to Role Management: {ex.Message}", "OK");
            
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex}");
        }
    }

    private async void OnUserRoleAssignmentClicked(object sender, EventArgs e)
    {
        await AnimateButtonClick(sender as Button);
        
        try
        {
            await _navigationService.NavigateToUserRoleManagementAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Navigation Error", 
                $"Could not navigate to User Role Management: {ex.Message}", "OK");
            
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex}");
        }
    }
    
    private async Task AnimateButtonClick(View button)
    {
        if (button == null) return;
        
        // Scale down
        await button.ScaleTo(0.95, 50, Easing.CubicOut);
        
        // Scale back up
        await button.ScaleTo(1, 50, Easing.CubicIn);
    }
}