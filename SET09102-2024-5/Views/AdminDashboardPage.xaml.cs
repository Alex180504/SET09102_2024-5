using Microsoft.Maui.Controls;
using SET09102_2024_5.Services;
using System;
using System.Threading.Tasks;

namespace SET09102_2024_5.Views;

public partial class AdminDashboardPage : ViewBase
{
    private readonly INavigationService _navigationService;

    public AdminDashboardPage(INavigationService navigationService)
    {
        InitializeComponent();
        _navigationService = navigationService;
    }

    private async void OnRoleManagementClicked(object sender, EventArgs e)
    {
        await AnimateControlSelection(sender as View);
        await _navigationService.NavigateToAsync("RoleManagementPage");
    }

    private async void OnUserRoleAssignmentClicked(object sender, EventArgs e)
    {
        await AnimateControlSelection(sender as View);
        await _navigationService.NavigateToAsync("UserRoleManagementPage");
    }
}