using System.Text.Json;

namespace BusTicket.Core;

/// <summary>Compte utilisateur tel qu'il est stocké (jamais envoyé tel quel : voir <see cref="ToDto"/>).</summary>
public sealed class UserRecord
{
    public string Id { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    /// <summary>Les 9 derniers chiffres du téléphone (sans indicatif), pour la connexion par numéro.</summary>
    public string PhoneDigits { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public UserDto ToDto() => new(Id, FullName, Email, Phone);
}

/// <summary>Billet tel qu'il est stocké. Le trajet est recopié : le billet reste lisible même si les horaires changent.</summary>
public sealed class BookingRecord
{
    public string Reference { get; set; } = "";
    public string UserId { get; set; } = "";
    public string TripId { get; set; } = "";
    public string Company { get; set; } = "";
    public string ServiceClass { get; set; } = "";
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public DateTime Departure { get; set; }
    public DateTime Arrival { get; set; }
    public int UnitPrice { get; set; }
    public int Stops { get; set; }
    /// <summary>Numéros des sièges séparés par des virgules : « 3,4 ».</summary>
    public string Seats { get; set; } = "";
    public string PassengerName { get; set; } = "";
    public PaymentMethodKind PaymentMethod { get; set; }
    public string AccountMasked { get; set; } = "";
    public int TicketsAmount { get; set; }
    public int ServiceFee { get; set; }
    public int Total { get; set; }
    public string TransactionId { get; set; } = "";
    public DateTime PaidAt { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime? BoardedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public IReadOnlyList<int> SeatNumbers =>
        Seats.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
}

/// <summary>
/// Accès aux données. Deux implémentations : SQLite sur le serveur,
/// <see cref="JsonFileDataStore"/> sur le téléphone (mode hors ligne).
/// </summary>
public interface IDataStore
{
    UserRecord? GetUser(string id);
    UserRecord? FindUserByEmail(string email);
    UserRecord? FindUserByPhone(string phoneDigits);
    void AddUser(UserRecord user);
    void UpdateUser(UserRecord user);

    BookingRecord? FindBooking(string reference);
    IReadOnlyList<BookingRecord> BookingsOfUser(string userId);
    /// <summary>Sièges déjà vendus par BusTicket sur ce trajet.</summary>
    IReadOnlyList<int> SoldSeats(string tripId);
    void AddBooking(BookingRecord booking);
    void UpdateBooking(BookingRecord booking);
}

/// <summary>Toutes les données dans un seul fichier JSON. Suffisant pour quelques centaines de billets sur un téléphone.</summary>
public sealed class JsonFileDataStore : IDataStore
{
    private sealed class Data
    {
        public List<UserRecord> Users { get; set; } = [];
        public List<BookingRecord> Bookings { get; set; } = [];
    }

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    private readonly string _path;
    private readonly Lock _gate = new();
    private readonly Data _data;

    public JsonFileDataStore(string path)
    {
        _path = path;
        _data = Load(path);
    }

    private static Data Load(string path)
    {
        try
        {
            return File.Exists(path) ? JsonSerializer.Deserialize<Data>(File.ReadAllText(path), Options) ?? new() : new();
        }
        catch (Exception) // fichier corrompu ou d'une ancienne version : on repart de zéro
        {
            return new();
        }
    }

    private void Save()
    {
        try
        {
            var temp = _path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(_data, Options));
            File.Move(temp, _path, overwrite: true);
        }
        catch (IOException)
        {
            // Disque plein ou inaccessible : on continue avec les données en mémoire.
        }
    }

    public UserRecord? GetUser(string id) { lock (_gate) return _data.Users.FirstOrDefault(u => u.Id == id); }

    public UserRecord? FindUserByEmail(string email)
    {
        lock (_gate) return _data.Users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
    }

    public UserRecord? FindUserByPhone(string phoneDigits)
    {
        lock (_gate) return _data.Users.FirstOrDefault(u => u.PhoneDigits.Length > 0 && u.PhoneDigits == phoneDigits);
    }

    public void AddUser(UserRecord user) { lock (_gate) { _data.Users.Add(user); Save(); } }

    public void UpdateUser(UserRecord user) { lock (_gate) Save(); }

    public BookingRecord? FindBooking(string reference)
    {
        lock (_gate) return _data.Bookings.FirstOrDefault(b => string.Equals(b.Reference, reference, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<BookingRecord> BookingsOfUser(string userId)
    {
        lock (_gate) return _data.Bookings.Where(b => b.UserId == userId).ToList();
    }

    public IReadOnlyList<int> SoldSeats(string tripId)
    {
        lock (_gate) return _data.Bookings.Where(b => b.TripId == tripId).SelectMany(b => b.SeatNumbers).ToList();
    }

    public void AddBooking(BookingRecord booking) { lock (_gate) { _data.Bookings.Add(booking); Save(); } }

    public void UpdateBooking(BookingRecord booking) { lock (_gate) Save(); }
}
