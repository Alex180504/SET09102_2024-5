using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace SET09102_2024_5.Views;

public partial class AdminDashboardPage : ContentPage
{
    public AdminDashboardPage()
    {
        InitializeComponent();
    }

    // Apply animations when the page appears
    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            
            // Start all animations with elements initially invisible
            this.Opacity = 0;
            
            // Fade in the entire page
            await this.FadeTo(1, 250, Easing.CubicOut);
            
            // Additional initialization if needed
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred while loading the Admin Dashboard: {ex.Message}", "OK");
        }
    }

    private async void OnRoleManagementClicked(object sender, EventArgs e)
    {
        await AnimateButtonClick(sender as Button);
        
        try
        {
            // Use absolute path for navigation
            await Shell.Current.GoToAsync($"///{nameof(RoleManagementPage)}");
        }
        catch (Exception ex)
        {
            // Display error to help with debugging
            await DisplayAlert("Navigation Error", $"Could not navigate to Role Management: {ex.Message}", "OK");
        }
    }

    private async void OnUserRoleAssignmentClicked(object sender, EventArgs e)
    {
        await AnimateButtonClick(sender as Button);
        
        try
        {
            // Use absolute path for navigation
            await Shell.Current.GoToAsync($"///{nameof(UserRoleManagementPage)}");
        }
        catch (Exception ex)
        {
            // Display error to help with debugging
            await DisplayAlert("Navigation Error", $"Could not navigate to User Role Management: {ex.Message}", "OK");
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