using SET09102_2024_5.ViewModels;

namespace SET09102_2024_5.Views;

public partial class MaintenanceListPage : ContentPage
{
    public MaintenanceListPage(MaintenanceListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Trigger a reload of the tasks each time this page appears
        if (BindingContext is MaintenanceListViewModel vm)
        {
            // If you have a RefreshView, you may want to pass 'false' to avoid spinner;
            // otherwise, just call the same load command.
            vm.LoadTasksCommand.Execute(null);
        }
    }
}