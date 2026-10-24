using BusTicket.Services;
using BusTicket.ViewModels;

namespace BusTicket.Views.Tickets;

public partial class MyTicketsPage : ContentPage
{
    private readonly MyTicketsViewModel _viewModel;
    private readonly BookingRepository _bookings;

    public MyTicketsPage(MyTicketsViewModel viewModel, BookingRepository bookings)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _bookings = bookings;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Refresh();              // un billet a pu expirer depuis le dernier affichage
        await _bookings.RefreshAsync();    // puis mise à jour depuis le serveur, en arrière-plan
    }
}
