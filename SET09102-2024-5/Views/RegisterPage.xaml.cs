using SET09102_2024_5.ViewModels;

namespace SET09102_2024_5.Views;

public partial class RegisterPage : ViewBase
{
    public RegisterPage(RegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}