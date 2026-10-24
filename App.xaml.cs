using System.Globalization;
using BusTicket.Services;

namespace BusTicket;

public partial class App : Application
{
    private readonly Backend _backend;
    private readonly BookingRepository _bookings;

    public App(Backend backend, BookingRepository bookings)
    {
        _backend = backend;
        _bookings = bookings;

        // Dates (sélecteur de date, etc.) en français, quelle que soit la langue du téléphone.
        var french = new CultureInfo("fr-FR");
        CultureInfo.DefaultThreadCurrentCulture = french;
        CultureInfo.DefaultThreadCurrentUICulture = french;

        InitializeComponent();
        UserAppTheme = AppTheme.Light; // la maquette n'a pas de thème sombre
    }

    /// <summary>Si une session est enregistrée, on ouvre directement les onglets au lieu de l'écran de connexion.</summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        if (_backend.IsLoggedIn) _ = _bookings.RefreshAsync();
        return new(new AppShell(startLoggedIn: _backend.IsLoggedIn)) { Title = "BusTicket" };
    }
}
