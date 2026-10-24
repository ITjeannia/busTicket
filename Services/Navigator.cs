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
    public const string Profile = "//main/profile";

    // Pages empilées
    public const string Register = "register";
    public const string Results = "results";
    public const string Seats = "seats";
    public const string Payment = "payment";
    public const string Receipt = "receipt";
    public const string Ticket = "ticket";
    public const string Scanner = "scanner";
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

    /// <summary>Après un paiement : vide la pile de réservation et ouvre le reçu (retour = accueil).</summary>
    public Task ShowReceiptAsync(IDictionary<string, object> parameters) =>
        GoToAsync($"{Routes.Search}/{Routes.Receipt}", parameters);

    /// <summary>Ouvre un billet dans l'onglet « Mes billets ».</summary>
    public Task ShowTicketAsync(IDictionary<string, object> parameters) =>
        GoToAsync($"{Routes.Tickets}/{Routes.Ticket}", parameters);

    public Task InfoAsync(string message, string title = "BusTicket") => Shell.DisplayAlertAsync(title, message, "OK");

    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel = "Annuler") =>
        Shell.DisplayAlertAsync(title, message, accept, cancel);

    /// <summary>Liste de choix (villes, passagers). Renvoie null si annulé.</summary>
    public async Task<string?> PickAsync(string title, IEnumerable<string> options)
    {
        var choice = await Shell.DisplayActionSheetAsync(title, "Annuler", null, options.ToArray());
        return choice is null or "Annuler" ? null : choice;
    }

    /// <summary>Saisie d'un texte. Renvoie null si annulé.</summary>
    public Task<string?> PromptAsync(string title, string message, string initialValue = "", Keyboard? keyboard = null) =>
        Shell.DisplayPromptAsync(title, message, "Valider", "Annuler", initialValue: initialValue, keyboard: keyboard ?? Keyboard.Default);

    public const string ComingSoon = "Fonctionnalité bientôt disponible";
}
