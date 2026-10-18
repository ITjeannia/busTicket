using BusTicket.ViewModels;

namespace BusTicket.Views.Tickets;

public partial class TicketPage : ContentPage
{
    public TicketPage(TicketViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
