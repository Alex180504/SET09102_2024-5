using SET09102_2024_5.ViewModels;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;

namespace SET09102_2024_5.Views;

public partial class RoleManagementPage : ViewBase
{
    private readonly RoleManagementViewModel _viewModel;
    
    public RoleManagementPage(RoleManagementViewModel viewModel)
    {
        InitializeComponent();  // Add this line to initialize the UI components
        _viewModel = viewModel;
        BindingContext = _viewModel;
        
        // Subscribe to property changes to handle UI updates
        _viewModel.PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == nameof(RoleManagementViewModel.SelectedRole) ||
                e.PropertyName == nameof(RoleManagementViewModel.IsUsersTabSelected))
            {
                // Force update bindings when critical properties change
                MainThread.BeginInvokeOnMainThread(() => 
                {
                    System.Diagnostics.Debug.WriteLine($"Forcing UI update due to {e.PropertyName} change");
                    UpdateChildrenLayout();
                });
            }
        };
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Force IsBusy to false first to reset any stuck state
        _viewModel.IsBusy = false;
        
        try
        {
            // Explicitly load data when the page appears
            await _viewModel.InitializeDataAsync();
            
            // Reset to show privileges tab if a role is selected
            if (_viewModel.SelectedRole != null)
            {
                _viewModel.IsUsersTabSelected = false;
                System.Diagnostics.Debug.WriteLine("OnAppearing: Setting to privileges tab for selected role");
                
                // Force layout update
                UpdateChildrenLayout();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnAppearing: {ex.Message}");
        }
        finally
        {
            // Ensure IsBusy is definitely set to false
            _viewModel.IsBusy = false;
        }
    }
    
    // Animation helper for card selection
    public async Task AnimateCardSelection(View card)
    {
        await AnimateControlSelection(card);
    }
}