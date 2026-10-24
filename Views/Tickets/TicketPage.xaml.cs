using BusTicket.ViewModels;

namespace BusTicket.Views.Tickets;

public partial class TicketPage : ContentPage
{
    public TicketPage(TicketViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        // Les encoches se placent à hauteur de la ligne de perforation.
        Perforation.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(Y)) return;
            var top = Perforation.Y - NotchLeft.HeightRequest / 2 + 1;
            NotchLeft.Margin = new Thickness(-11, top, 0, 0);
            NotchRight.Margin = new Thickness(0, top, -11, 0);
        };
    }

    /// <summary>Le QR code « respire » une fois à l'ouverture pour attirer l'œil.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!((TicketViewModel)BindingContext).IsValid) return;
        await Task.Delay(450);
        await QrFrame.ScaleToAsync(1.04, 180, Easing.CubicOut);
        await QrFrame.ScaleToAsync(1, 260, Easing.SpringOut);
    }
}
