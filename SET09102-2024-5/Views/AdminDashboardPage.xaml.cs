using Microsoft.Maui.Controls;

namespace SET09102_2024_5.Views;

public partial class AdminDashboardPage : ContentPage
{
    public AdminDashboardPage()
    {
        InitializeComponent();
    }

    private async void OnRoleManagementClicked(object sender, EventArgs e)
    {
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

    // Add error handling for page initialization
    protected override void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            // Additional initialization if needed
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"An error occurred while loading the Admin Dashboard: {ex.Message}", "OK");
        }
    }
}