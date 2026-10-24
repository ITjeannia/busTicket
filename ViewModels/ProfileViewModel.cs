using BusTicket.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BusTicket.ViewModels;

/// <summary>Onglet « Profil » : compte, serveur, espace contrôleur, déconnexion.</summary>
public partial class ProfileViewModel : ObservableObject
{
    private readonly Backend _backend;
    private readonly BookingRepository _bookings;
    private readonly ServerSettings _server;
    private readonly ApiClient _api;
    private readonly Navigator _navigator;

    public ProfileViewModel(Backend backend, BookingRepository bookings, ServerSettings server, ApiClient api, Navigator navigator)
    {
        _backend = backend;
        _bookings = bookings;
        _server = server;
        _api = api;
        _navigator = navigator;
        _backend.StatusChanged += (_, _) => RefreshStatus();
    }

    [ObservableProperty] public partial string FullName { get; set; } = "";
    [ObservableProperty] public partial string Email { get; set; } = "";
    [ObservableProperty] public partial string Phone { get; set; } = "";
    [ObservableProperty] public partial string Initials { get; set; } = "";
    [ObservableProperty] public partial string ServerUrl { get; set; } = "";
    [ObservableProperty] public partial string ServerStatus { get; set; } = "";
    [ObservableProperty] public partial bool IsOnline { get; set; }
    /// <summary>Message affiché quand le compte n'existe que sur ce téléphone (créé hors ligne).</summary>
    [ObservableProperty] public partial string? LocalAccountMessage { get; set; }
    [ObservableProperty] public partial string TicketsCount { get; set; } = "0";
    [ObservableProperty] public partial string TripsCount { get; set; } = "0";

    public string Footer => $"{AppInfo.Name} {AppInfo.VersionString} · Paiement simulé (démonstration)";

    /// <summary>Appelé par la page à chaque affichage.</summary>
    public void Refresh()
    {
        var user = _backend.CurrentUser;
        FullName = user?.FullName ?? "";
        Email = user?.Email ?? "";
        Phone = user?.Phone ?? "";
        Initials = user?.Initials ?? "";
        LocalAccountMessage = _backend.Session is { Token: null }
            ? "Connexion faite hors ligne : ce compte n'est pas relié au serveur. Déconnectez-vous puis reconnectez-vous quand le serveur est joignable."
            : null;
        TicketsCount = _bookings.Upcoming.Count.ToString();
        TripsCount = _bookings.Past.Count.ToString();
        RefreshStatus();
        _ = _backend.CheckConnectionAsync();
    }

    private void RefreshStatus()
    {
        ServerUrl = _server.BaseUrl;
        IsOnline = !_backend.IsOffline;
        ServerStatus = IsOnline ? "Connecté au serveur" : "Hors ligne · données locales";
    }

    [RelayCommand]
    private async Task EditServerAsync()
    {
        var url = await _navigator.PromptAsync("Adresse du serveur",
            "Adresse de l'API BusTicket (ex. http://192.168.1.20:5080). Sur l'émulateur Android : http://10.0.2.2:5080.",
            _server.BaseUrl, Keyboard.Url);
        if (string.IsNullOrWhiteSpace(url)) return;

        var normalized = ServerSettings.Normalize(url);
        var reachable = await _api.PingAsync(normalized);
        if (!reachable && !await _navigator.ConfirmAsync("Serveur injoignable",
                $"{normalized} ne répond pas. Enregistrer quand même ? L'application fonctionnera hors ligne en attendant.",
                "Enregistrer"))
            return;

        _server.BaseUrl = normalized;
        await _backend.CheckConnectionAsync();
        RefreshStatus();
        if (reachable) await _bookings.RefreshAsync();
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        ServerStatus = "Test en cours…";
        var online = await _backend.CheckConnectionAsync();
        RefreshStatus();
        if (online) await _bookings.RefreshAsync();
    }

    [RelayCommand]
    private Task OpenScannerAsync() => _navigator.GoToAsync(Routes.Scanner);

    [RelayCommand]
    private Task OpenTicketsAsync() => _navigator.GoToAsync(Routes.Tickets);

    [RelayCommand]
    private Task HelpAsync() => _navigator.InfoAsync(
        "Service client BusTicket\n+237 6 99 00 00 00\nsupport@busticket.cm\n\nDu lundi au samedi, 6 h – 21 h.",
        "Aide et contact");

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (!await _navigator.ConfirmAsync("Se déconnecter", "Vos billets seront de nouveau disponibles à la prochaine connexion.", "Se déconnecter"))
            return;
        _backend.Logout();
        _bookings.Clear();
        await _navigator.GoToAsync(Routes.Login);
    }
}
