using BusTicket.Core;

namespace BusTicket.Services;

/// <summary>Session enregistrée sur le téléphone. Token null = compte créé hors ligne (connu de ce téléphone seulement).</summary>
public sealed record Session(UserDto User, string? Token);

/// <summary>
/// Point d'accès unique aux données pour les écrans.
/// Chaque opération est d'abord envoyée au serveur ; s'il est injoignable, le même moteur
/// (<see cref="BookingEngine"/>) tourne sur le téléphone avec une base locale : l'application
/// ne bloque jamais, même sans réseau. Les erreurs métier (mot de passe faux, siège pris…)
/// remontent telles quelles, en ligne comme hors ligne.
/// </summary>
public sealed class Backend
{
    private const string SessionFile = "session";
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(15);

    private readonly ApiClient _api;
    private readonly LocalStore _store;
    private readonly BookingEngine _local;
    private DateTime _retryAfter = DateTime.MinValue;

    public Backend(ApiClient api, LocalStore store)
    {
        _api = api;
        _store = store;
        _local = new BookingEngine(new JsonFileDataStore(LocalStore.PathOf("local_database")));
        _local.EnsureDemoAccount();

        Session = store.Load<Session>(SessionFile);
        _api.Token = Session?.Token;
    }

    public Session? Session { get; private set; }
    public UserDto? CurrentUser => Session?.User;
    public bool IsLoggedIn => Session is not null;

    /// <summary>Vrai quand la dernière tentative de joindre le serveur a échoué.</summary>
    public bool IsOffline { get; private set; }

    public event EventHandler? StatusChanged;

    public IReadOnlyList<string> Cities => Catalog.Cities;

    // ===== Connexion =====

    public async Task LoginAsync(string identifier, string password)
    {
        var request = new LoginRequest(identifier, password);
        var session = await RunAsync(
            async () =>
            {
                var auth = await _api.PostAsync<AuthResponse>("auth/login", request);
                _local.MirrorUser(auth.User, password);   // pour pouvoir se reconnecter hors ligne
                return new Session(auth.User, auth.Token);
            },
            () => new Session(_local.Login(request), null));
        SetSession(session);
    }

    public async Task RegisterAsync(RegisterRequest request)
    {
        var session = await RunAsync(
            async () =>
            {
                var auth = await _api.PostAsync<AuthResponse>("auth/register", request);
                _local.MirrorUser(auth.User, request.Password);
                return new Session(auth.User, auth.Token);
            },
            () => new Session(_local.Register(request), null));
        SetSession(session);
    }

    public void Logout()
    {
        Session = null;
        _api.Token = null;
        _store.Delete(SessionFile);
    }

    private void SetSession(Session session)
    {
        Session = session;
        _api.Token = session.Token;
        _store.Save(SessionFile, session);
    }

    // ===== Trajets =====

    public Task<IReadOnlyList<TripDto>> SearchAsync(string from, string to, DateTime date) =>
        RunAsync(
            () => _api.GetAsync<IReadOnlyList<TripDto>>(
                $"trips?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}&date={date:yyyy-MM-dd}"),
            () => _local.Search(from, to, date));

    public Task<TripDto> GetTripAsync(string tripId) =>
        RunAsync(() => _api.GetAsync<TripDto>($"trips/{Uri.EscapeDataString(tripId)}"), () => _local.GetTrip(tripId));

    // ===== Billets =====

    /// <summary>Réserve et paie (paiement simulé, toujours accepté).</summary>
    public Task<BookingDto> BookAsync(CreateBookingRequest request) =>
        RunAsync(
            () => _api.PostAsync<BookingDto>("bookings", request),
            () =>
            {
                var user = RequireUser();
                _local.MirrorUser(user, null);
                return _local.Book(user.Id, request);
            },
            needsServerAccount: true);

    /// <summary>Billets du serveur + billets achetés hors ligne sur ce téléphone.</summary>
    public async Task<IReadOnlyList<BookingDto>> GetBookingsAsync()
    {
        var user = RequireUser();
        var local = _local.BookingsOf(user.Id);
        var remote = await RunAsync(() => _api.GetAsync<IReadOnlyList<BookingDto>>("bookings"), () => [], needsServerAccount: true);
        return remote.Concat(local.Where(l => remote.All(r => r.Reference != l.Reference))).ToList();
    }

    // ===== Contrôle =====

    public Task<VerificationDto> VerifyAsync(string payload, bool board) =>
        RunAsync(
            () => _api.PostAsync<VerificationDto>("tickets/verify", new VerifyRequest(payload, board)),
            () => _local.Verify(payload, board),
            needsServerAccount: true);

    // ===== Serveur =====

    /// <summary>Teste le serveur maintenant (au lieu d'attendre le prochain essai automatique).</summary>
    public async Task<bool> CheckConnectionAsync()
    {
        var online = await _api.PingAsync();
        _retryAfter = online ? DateTime.MinValue : DateTime.UtcNow.Add(RetryDelay);
        SetOffline(!online);
        return online;
    }

    private UserDto RequireUser() =>
        CurrentUser ?? throw new BusTicketException(ErrorKind.Unauthorized, "Connectez-vous pour continuer.");

    /// <summary>
    /// Essaie le serveur, sinon exécute la version locale. Après un échec réseau, on n'insiste pas
    /// pendant 15 s pour ne pas faire attendre l'utilisateur à chaque écran.
    /// </summary>
    private async Task<T> RunAsync<T>(Func<Task<T>> remote, Func<T> local, bool needsServerAccount = false)
    {
        var serverAccount = !needsServerAccount || Session?.Token is not null;
        if (serverAccount && DateTime.UtcNow >= _retryAfter)
        {
            try
            {
                var result = await remote();
                SetOffline(false);
                return result;
            }
            catch (ServerUnreachableException)
            {
                _retryAfter = DateTime.UtcNow.Add(RetryDelay);
                SetOffline(true);
            }
        }
        return await Task.Run(local);
    }

    private void SetOffline(bool offline)
    {
        if (IsOffline == offline) return;
        IsOffline = offline;
        MainThread.BeginInvokeOnMainThread(() => StatusChanged?.Invoke(this, EventArgs.Empty));
    }
}
