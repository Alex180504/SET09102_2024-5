using SET09102_2024_5.ViewModels;

namespace SET09102_2024_5.Views;

public partial class LoginPage : ViewBase
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}