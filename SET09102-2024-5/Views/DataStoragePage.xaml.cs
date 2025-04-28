using SET09102_2024_5.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace SET09102_2024_5.Views;

public partial class DataStoragePage : ViewBase
{
    // Add parameterless constructor for Shell navigation
    public DataStoragePage()
    {
        InitializeComponent();
        BindingContext = App.Current.Handler.MauiContext?.Services.GetService<DataStorageViewModel>();
    }

    public DataStoragePage(DataStorageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is DataStorageViewModel vm)
        {
            await vm.LoadBackupsAsync();
        }
    }
}