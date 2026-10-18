namespace BusTicket.Services;

/// <summary>
/// Routes de l'application et accès à la navigation Shell.
/// Pour ajouter un écran : une constante ici + Routing.RegisterRoute dans AppShell.xaml.cs.
/// </summary>
public static class Routes
{
    // Pages racines (déclarées dans AppShell.xaml)
    public const string Login = "//login";
    public const string Search = "//main/search";
    public const string Tickets = "//main/tickets";

    // Pages empilées
    public const string Register = "register";
    public const string Results = "results";
    public const string Seats = "seats";
    public const string Payment = "payment";
    public const string Ticket = "ticket";
}

/// <summary>Navigation et boîtes de dialogue, regroupées pour que les ViewModels ne dépendent pas des pages.</summary>
public class Navigator
{
    private static Shell Shell => Shell.Current;

    public Task GoToAsync(string route, IDictionary<string, object>? parameters = null) =>
        parameters is null
            ? Shell.GoToAsync(route)
            : Shell.GoToAsync(route, new ShellNavigationQueryParameters(parameters));

    public Task BackAsync() => Shell.GoToAsync("..");

    /// <summary>Après un paiement : vide la pile de réservation puis ouvre le billet dans l'onglet « Mes billets ».</summary>
    public async Task ShowNewTicketAsync(IDictionary<string, object> parameters)
    {
        await Shell.Navigation.PopToRootAsync(false);
        await GoToAsync($"{Routes.Tickets}/{Routes.Ticket}", parameters);
    }

    public Task InfoAsync(string message) => Shell.DisplayAlertAsync("BusTicket", message, "OK");

    /// <summary>Liste de choix (villes, passagers). Renvoie null si annulé.</summary>
    public async Task<string?> PickAsync(string title, IEnumerable<string> options)
    {
        var choice = await Shell.DisplayActionSheetAsync(title, "Annuler", null, options.ToArray());
        return choice is null or "Annuler" ? null : choice;
    }

    public const string ComingSoon = "Fonctionnalité bientôt disponible";
}
