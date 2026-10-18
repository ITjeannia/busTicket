using BusTicket.ViewModels;

namespace BusTicket.Views.Tickets;

public partial class MyTicketsPage : ContentPage
{
    private readonly MyTicketsViewModel _viewModel;

    public MyTicketsPage(MyTicketsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Refresh(); // un billet a pu expirer depuis le dernier affichage
    }
}
