using BusTicket.Services;
using BusTicket.Views.Auth;
using BusTicket.Views.Booking;
using BusTicket.Views.Search;
using BusTicket.Views.Tickets;

namespace BusTicket;

public partial class AppShell : Shell
{
    public AppShell(bool startLoggedIn)
    {
        InitializeComponent();

        // Items[0] = connexion, Items[1] = onglets (voir AppShell.xaml).
        if (startLoggedIn) CurrentItem = Items[1];

        // Écrans empilés au-dessus des onglets. Pour ajouter un écran : une constante dans Routes + une ligne ici.
        Routing.RegisterRoute(Routes.Register, typeof(RegisterPage));
        Routing.RegisterRoute(Routes.Results, typeof(ResultsPage));
        Routing.RegisterRoute(Routes.Seats, typeof(SeatSelectionPage));
        Routing.RegisterRoute(Routes.Payment, typeof(PaymentPage));
        Routing.RegisterRoute(Routes.Receipt, typeof(ReceiptPage));
        Routing.RegisterRoute(Routes.Ticket, typeof(TicketPage));
        Routing.RegisterRoute(Routes.Scanner, typeof(ScannerPage));
    }
}
