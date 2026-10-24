using BusTicket.Core;

namespace BusTicket.Services;

/// <summary>
/// Billets de l'utilisateur. Une copie est gardée sur le téléphone : les billets (et leur QR code)
/// restent consultables sans réseau. Les écrans s'abonnent à <see cref="Changed"/> pour se mettre à jour.
/// </summary>
public class BookingRepository
{
    private const string StoreName = "tickets";

    private readonly Backend _backend;
    private readonly LocalStore _store;
    private List<BookingDto> _tickets;

    public BookingRepository(Backend backend, LocalStore store)
    {
        _backend = backend;
        _store = store;
        _tickets = store.Load<List<BookingDto>>(StoreName) ?? [];
    }

    public event EventHandler? Changed;

    /// <summary>À venir (y compris embarqués, jusqu'à l'arrivée), du plus proche au plus lointain.</summary>
    public IReadOnlyList<BookingDto> Upcoming => _tickets.Where(b => !b.IsExpired).OrderBy(b => b.Trip.Departure).ToList();

    public IReadOnlyList<BookingDto> Past => _tickets.Where(b => b.IsExpired).OrderByDescending(b => b.Trip.Departure).ToList();

    public BookingDto? Find(string reference) => _tickets.FirstOrDefault(b => b.Reference == reference);

    /// <summary>Recharge depuis le serveur (ou la base locale). En cas d'échec, on garde la copie existante.</summary>
    public async Task RefreshAsync()
    {
        if (!_backend.IsLoggedIn) return;
        try
        {
            var fresh = await _backend.GetBookingsAsync();
            // On garde aussi les billets en cache absents de la réponse (achetés sur un autre mode de connexion).
            _tickets = fresh.Concat(_tickets.Where(c => fresh.All(f => f.Reference != c.Reference))).ToList();
            Save();
        }
        catch (BusTicketException)
        {
            // Session expirée côté serveur, etc. : la copie locale reste affichée.
        }
    }

    /// <summary>Réserve et paie. Le billet est ajouté en tête de liste et enregistré.</summary>
    public async Task<BookingDto> BookAsync(CreateBookingRequest request)
    {
        var booking = await _backend.BookAsync(request);
        _tickets.RemoveAll(b => b.Reference == booking.Reference);
        _tickets.Insert(0, booking);
        Save();
        return booking;
    }

    /// <summary>À la déconnexion : les billets d'un compte ne doivent pas apparaître dans un autre.</summary>
    public void Clear()
    {
        _tickets = [];
        _store.Delete(StoreName);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Save()
    {
        _store.Save(StoreName, _tickets);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
