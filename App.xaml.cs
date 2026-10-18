using System.Globalization;

namespace BusTicket;

public partial class App : Application
{
    public App()
    {
        // Dates (sélecteur de date, etc.) en français, quelle que soit la langue du téléphone.
        var french = new CultureInfo("fr-FR");
        CultureInfo.DefaultThreadCurrentCulture = french;
        CultureInfo.DefaultThreadCurrentUICulture = french;

        InitializeComponent();
        UserAppTheme = AppTheme.Light; // la maquette n'a pas encore de thème sombre
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new AppShell()) { Title = "BusTicket" };
}
