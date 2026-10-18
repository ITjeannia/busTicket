using BusTicket.ViewModels;

namespace BusTicket.Views.Booking;

public partial class SeatSelectionPage : ContentPage
{
    public SeatSelectionPage(SeatSelectionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
