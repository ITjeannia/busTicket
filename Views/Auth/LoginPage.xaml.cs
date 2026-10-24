using BusTicket.Controls;
using BusTicket.ViewModels;

namespace BusTicket.Views.Auth;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        viewModel.Failed += async (_, _) => await Motion.ShakeAsync(Form);
    }
}
