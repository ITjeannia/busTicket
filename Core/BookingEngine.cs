using System.Security.Cryptography;

namespace BusTicket.Core;

public enum ErrorKind { Validation, Unauthorized, NotFound, Conflict }

/// <summary>Erreur « métier » dont le message peut être affiché tel quel à l'utilisateur.</summary>
public sealed class BusTicketException(ErrorKind kind, string message) : Exception(message)
{
    public ErrorKind Kind { get; } = kind;
}

/// <summary>
/// Règles de BusTicket : comptes, recherche, réservation + paiement, contrôle des billets.
/// Utilisé par le serveur et, hors ligne, par l'application (avec un autre <see cref="IDataStore"/>).
/// </summary>
public sealed class BookingEngine(IDataStore store, Func<DateTime>? clock = null)
{
    public const string DemoEmail = "jean.dupont@mail.com";
    public const string DemoPassword = "voyage123";

    // Un seul verrou : la vérification « siège libre » et l'enregistrement du billet ne doivent pas s'entrelacer.
    private static readonly Lock Gate = new();

    private DateTime Now => clock?.Invoke() ?? DateTime.Now;

    // ===== Comptes =====

    /// <summary>Crée le compte de démonstration s'il n'existe pas.</summary>
    public void EnsureDemoAccount()
    {
        if (store.FindUserByEmail(DemoEmail) is not null) return;
        store.AddUser(NewUser("demo-jean-dupont", "Jean Dupont", DemoEmail, "+237 6 90 12 34 56", DemoPassword));
    }

    public UserDto Register(RegisterRequest request)
    {
        var errors = new[]
        {
            Validators.Required(request.FullName),
            Validators.Email(request.Email),
            Validators.Phone(request.Phone),
            Validators.Password(request.Password),
        };
        if (errors.FirstOrDefault(e => e is not null) is { } error) throw Invalid(error);

        var email = request.Email.Trim();
        lock (Gate)
        {
            if (store.FindUserByEmail(email) is not null)
                throw new BusTicketException(ErrorKind.Conflict, "Un compte existe déjà avec cette adresse e-mail.");
            if (store.FindUserByPhone(PhoneKey(request.Phone)) is not null)
                throw new BusTicketException(ErrorKind.Conflict, "Un compte existe déjà avec ce numéro de téléphone.");

            var user = NewUser(Guid.NewGuid().ToString("N"), request.FullName.Trim(), email, request.Phone.Trim(), request.Password);
            store.AddUser(user);
            return user.ToDto();
        }
    }

    public UserDto Login(LoginRequest request)
    {
        var identifier = request.Identifier.Trim();
        var user = identifier.Contains('@')
            ? store.FindUserByEmail(identifier)
            : store.FindUserByPhone(PhoneKey(identifier));

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            throw new BusTicketException(ErrorKind.Unauthorized, "Identifiant ou mot de passe incorrect.");
        return user.ToDto();
    }

    public UserDto? GetUser(string id) => store.GetUser(id)?.ToDto();

    /// <summary>
    /// Copie locale d'un compte du serveur (application) : permet de se reconnecter
    /// et de réserver hors ligne avec le même compte.
    /// </summary>
    public void MirrorUser(UserDto user, string? password)
    {
        lock (Gate)
        {
            var existing = store.GetUser(user.Id) ?? store.FindUserByEmail(user.Email);
            if (existing is null)
            {
                store.AddUser(NewUser(user.Id, user.FullName, user.Email, user.Phone, password ?? Guid.NewGuid().ToString()));
                return;
            }
            existing.FullName = user.FullName;
            existing.Phone = user.Phone;
            existing.PhoneDigits = PhoneKey(user.Phone);
            if (password is not null) existing.PasswordHash = PasswordHasher.Hash(password);
            store.UpdateUser(existing);
        }
    }

    private UserRecord NewUser(string id, string fullName, string email, string phone, string password) => new()
    {
        Id = id,
        FullName = fullName,
        Email = email,
        Phone = phone,
        PhoneDigits = PhoneKey(phone),
        PasswordHash = PasswordHasher.Hash(password),
        CreatedAt = Now,
    };

    /// <summary>« +237 6 90 12 34 56 » et « 690123456 » donnent la même clé.</summary>
    private static string PhoneKey(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.Length > 9 ? digits[^9..] : digits;
    }

    // ===== Trajets =====

    public IReadOnlyList<string> Cities => Catalog.Cities;

    /// <summary>Départs encore en vente pour ce jour, avec les sièges réellement disponibles.</summary>
    public IReadOnlyList<TripDto> Search(string from, string to, DateTime date)
    {
        if (!Catalog.IsCity(from) || !Catalog.IsCity(to)) throw Invalid("Ville inconnue.");
        if (from == to) throw Invalid("Choisissez une ville d'arrivée différente du départ.");
        if (date.Date < Now.Date) throw Invalid("Cette date est passée.");

        return Catalog.TripsFor(from, to, date)
            .Where(IsOnSale)
            .Select(WithSoldSeats)
            .ToList();
    }

    public TripDto GetTrip(string tripId) =>
        WithSoldSeats(Catalog.FindTrip(tripId) ?? throw new BusTicketException(ErrorKind.NotFound, "Ce départ n'existe pas."));

    private bool IsOnSale(TripDto trip) => trip.Departure - Catalog.SalesCloseBeforeDeparture > Now;

    private TripDto WithSoldSeats(TripDto trip) =>
        trip with { BookedSeats = trip.BookedSeats.Union(store.SoldSeats(trip.Id)).Order().ToList() };

    // ===== Réservation et paiement =====

    /// <summary>
    /// Réserve les sièges et encaisse le paiement. Le paiement est simulé : il est toujours accepté,
    /// quel que soit le numéro saisi (aucune passerelle de paiement n'est branchée).
    /// </summary>
    public BookingDto Book(string userId, CreateBookingRequest request)
    {
        var user = store.GetUser(userId) ?? throw new BusTicketException(ErrorKind.Unauthorized, "Session expirée. Reconnectez-vous.");
        var seats = request.Seats.Distinct().Order().ToList();
        if (seats.Count == 0) throw Invalid("Choisissez au moins un siège.");
        if (seats.Count > Catalog.MaxSeatsPerBooking) throw Invalid($"{Catalog.MaxSeatsPerBooking} sièges maximum par réservation.");
        if (string.IsNullOrWhiteSpace(request.AccountNumber)) throw Invalid("Saisissez le numéro à débiter.");

        lock (Gate)
        {
            var trip = GetTrip(request.TripId);
            if (!IsOnSale(trip)) throw new BusTicketException(ErrorKind.Conflict, "Les ventes sont fermées pour ce départ.");
            if (seats.Any(s => s < 1 || s > trip.TotalSeats)) throw Invalid("Numéro de siège invalide.");

            var taken = seats.Intersect(trip.BookedSeats).ToList();
            if (taken.Count > 0)
                throw new BusTicketException(ErrorKind.Conflict,
                    $"{Formatters.Seats(taken)} : déjà réservé par un autre voyageur. Choisissez un autre siège.");

            var now = Now;
            var ticketsAmount = trip.Price * seats.Count;
            var record = new BookingRecord
            {
                Reference = NewReference(),
                UserId = user.Id,
                TripId = trip.Id,
                Company = trip.Company,
                ServiceClass = trip.ServiceClass,
                From = trip.From,
                To = trip.To,
                Departure = trip.Departure,
                Arrival = trip.Arrival,
                UnitPrice = trip.Price,
                Stops = trip.Stops,
                Seats = string.Join(',', seats),
                PassengerName = string.IsNullOrWhiteSpace(request.PassengerName) ? user.FullName : request.PassengerName.Trim(),
                PaymentMethod = request.Method,
                AccountMasked = Mask(request.AccountNumber),
                TicketsAmount = ticketsAmount,
                ServiceFee = Catalog.ServiceFee,
                Total = ticketsAmount + Catalog.ServiceFee,
                TransactionId = $"TX{now:yyMMddHHmm}{RandomNumberGenerator.GetInt32(1000, 9999)}",
                PaidAt = now,
                Status = BookingStatus.Confirmed,
                CreatedAt = now,
            };
            store.AddBooking(record);
            return ToDto(record);
        }
    }

    public IReadOnlyList<BookingDto> BookingsOf(string userId) =>
        store.BookingsOfUser(userId).OrderByDescending(b => b.Departure).Select(ToDto).ToList();

    public BookingDto GetBooking(string userId, string reference)
    {
        var record = store.FindBooking(reference);
        if (record is null || record.UserId != userId) throw new BusTicketException(ErrorKind.NotFound, "Billet introuvable.");
        return ToDto(record);
    }

    /// <summary>BT-7K3Q9P : sans 0/O ni 1/I pour être lu sans erreur au téléphone.</summary>
    private string NewReference()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        while (true)
        {
            var code = "BT-" + new string(Enumerable.Range(0, 6).Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]).ToArray());
            if (store.FindBooking(code) is null) return code;
        }
    }

    /// <summary>On ne garde que les 4 derniers caractères du compte débité : « •••• 3456 ».</summary>
    private static string Mask(string account)
    {
        var compact = new string(account.Where(c => !char.IsWhiteSpace(c)).ToArray());
        return compact.Length <= 4 ? compact : $"•••• {compact[^4..]}";
    }

    // ===== Contrôle à l'embarquement =====

    /// <summary>
    /// Contrôle un billet à partir du QR code (ou de la référence saisie).
    /// Avec <paramref name="board"/>, un billet valide est marqué « embarqué » : il ne pourra plus resservir.
    /// </summary>
    public VerificationDto Verify(string payload, bool board)
    {
        var text = payload.Trim();
        BookingRecord? record;
        TicketSigner.Payload? signed = null;

        if (text.StartsWith("BT-", StringComparison.OrdinalIgnoreCase))
        {
            record = store.FindBooking(text.ToUpperInvariant());
        }
        else
        {
            signed = TicketSigner.Read(text);
            if (signed is null)
                return Rejected("Code non reconnu", "Ce QR code n'est pas un billet BusTicket, ou il a été modifié.");
            record = store.FindBooking(signed.Reference);
        }

        if (record is null)
        {
            // Signature correcte mais billet absent de cette base : authentique, mais non vérifiable ici
            // (contrôle hors ligne sur un téléphone qui ne connaît pas ce billet).
            if (signed is not null && Catalog.FindTrip(signed.TripId) is { } trip)
                return Check(signed.Reference, trip, signed.Seats, null, BookingStatus.Confirmed, null, board: false, offline: true);
            return Rejected("Billet introuvable", "Aucun billet ne correspond à cette référence.");
        }

        if (signed is not null && (signed.TripId != record.TripId || !signed.Seats.SequenceEqual(record.SeatNumbers)))
            return Rejected("Code non reconnu", "Ce QR code ne correspond pas au billet enregistré.");

        lock (Gate)
        {
            var result = Check(record.Reference, ToDto(record).Trip, record.SeatNumbers, record.PassengerName,
                               record.Status, record.BoardedAt, board, offline: false);
            if (board && result.Result == VerificationResult.Valid)
            {
                record.Status = BookingStatus.Boarded;
                record.BoardedAt = Now;
                store.UpdateBooking(record);
                result = result with { Title = "Embarquement validé", Message = "Bon voyage !", BoardedAt = record.BoardedAt };
            }
            return result;
        }
    }

    private VerificationDto Check(string reference, TripDto trip, IReadOnlyList<int> seats, string? passenger,
                                  BookingStatus status, DateTime? boardedAt, bool board, bool offline)
    {
        VerificationDto Make(VerificationResult r, string title, string message) =>
            new(r, title, message, reference, trip, seats, passenger, boardedAt);

        if (trip.Arrival < Now)
            return Make(VerificationResult.Expired, "Billet expiré",
                $"Ce trajet est arrivé le {Formatters.DayMonth(trip.Arrival)} à {Formatters.Time(trip.Arrival)}.");
        if (status == BookingStatus.Boarded)
            return Make(VerificationResult.AlreadyBoarded, "Déjà utilisé",
                $"Ce billet a été contrôlé le {Formatters.Timestamp(boardedAt ?? Now)}.");
        if (trip.Departure.Date != Now.Date)
            return Make(VerificationResult.WrongDay, "Mauvaise date",
                $"Ce billet est valable pour le {Formatters.DayName(trip.Departure)} à {Formatters.Time(trip.Departure)}.");
        if (offline)
            return Make(VerificationResult.ValidOffline, "Billet authentique",
                "Contrôle hors ligne : la signature est valide, mais l'embarquement n'a pas pu être enregistré.");
        return Make(VerificationResult.Valid, "Billet valide",
            board ? "Bon voyage !" : "Vérifiez la pièce d'identité du voyageur, puis validez l'embarquement.");
    }

    private static VerificationDto Rejected(string title, string message) =>
        new(VerificationResult.Invalid, title, message, null, null, [], null, null);

    // ===== Conversion =====

    private static BookingDto ToDto(BookingRecord r)
    {
        var seats = r.SeatNumbers;
        var catalogTrip = Catalog.FindTrip(r.TripId);
        var trip = new TripDto(
            r.TripId, r.Company, r.ServiceClass, r.From, r.To, r.Departure, r.Arrival, r.UnitPrice, r.Stops,
            catalogTrip?.TotalSeats ?? 28, catalogTrip?.SeatsPerRow ?? 4, [], catalogTrip?.Amenities ?? []);
        var payment = new PaymentDto(r.PaymentMethod, r.AccountMasked, r.TicketsAmount, r.ServiceFee, r.Total, r.TransactionId, r.PaidAt);
        return new BookingDto(r.Reference, trip, seats, r.PassengerName, payment, r.Status, r.BoardedAt,
                              TicketSigner.Sign(r.Reference, r.TripId, seats), r.CreatedAt);
    }

    private static BusTicketException Invalid(string message) => new(ErrorKind.Validation, message);
}
