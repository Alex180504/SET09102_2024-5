using SET09102_2024_5.ViewModels;

namespace SET09102_2024_5.Views;

public partial class DataStoragePage : ContentPage
{
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