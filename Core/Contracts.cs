using System.Text.Json.Serialization;

namespace BusTicket.Core;

// Données échangées entre l'application et le serveur (JSON).
// Les propriétés calculées sont marquées [JsonIgnore] : elles ne transitent pas sur le réseau.

[JsonConverter(typeof(JsonStringEnumConverter<PaymentMethodKind>))]
public enum PaymentMethodKind { OrangeMoney, MtnMoMo, Card }

[JsonConverter(typeof(JsonStringEnumConverter<BookingStatus>))]
public enum BookingStatus { Confirmed, Boarded }

/// <summary>Résultat du contrôle d'un billet par un contrôleur.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<VerificationResult>))]
public enum VerificationResult
{
    /// <summary>Billet valide pour un départ aujourd'hui.</summary>
    Valid,
    /// <summary>Billet authentique, contrôlé hors ligne : la base n'a pas pu être consultée.</summary>
    ValidOffline,
    /// <summary>Billet valide mais pour un autre jour.</summary>
    WrongDay,
    /// <summary>Le voyageur a déjà embarqué avec ce billet.</summary>
    AlreadyBoarded,
    Expired,
    /// <summary>Code illisible, falsifié ou référence inconnue.</summary>
    Invalid,
}

public sealed record UserDto(string Id, string FullName, string Email, string Phone)
{
    [JsonIgnore]
    public string FirstName => FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";

    /// <summary>« Jean Dupont » → « JD »</summary>
    [JsonIgnore]
    public string Initials => string.Concat(
        FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));
}

public sealed record LoginRequest(string Identifier, string Password);

public sealed record RegisterRequest(string FullName, string Email, string Phone, string Password);

public sealed record AuthResponse(string Token, UserDto User);

/// <summary>Un départ proposé par une compagnie. <see cref="BookedSeats"/> = sièges déjà vendus.</summary>
public sealed record TripDto(
    string Id,
    string Company,
    string ServiceClass,
    string From,
    string To,
    DateTime Departure,
    DateTime Arrival,
    int Price,
    int Stops,
    int TotalSeats,
    int SeatsPerRow,
    IReadOnlyList<int> BookedSeats,
    IReadOnlyList<string> Amenities)
{
    [JsonIgnore] public TimeSpan Duration => Arrival - Departure;
    [JsonIgnore] public int AvailableSeats => TotalSeats - BookedSeats.Count;
    [JsonIgnore] public bool IsFull => AvailableSeats <= 0;
    [JsonIgnore] public bool IsVip => ServiceClass == Catalog.Vip;
    /// <summary>Départ de nuit : le bus arrive le lendemain.</summary>
    [JsonIgnore] public bool ArrivesNextDay => Arrival.Date > Departure.Date;
    [JsonIgnore] public string Route => $"{From} → {To}";

    [JsonIgnore]
    public string StopsLabel => Stops switch
    {
        0 => "Direct",
        1 => "1 arrêt",
        _ => $"{Stops} arrêts",
    };
}

public sealed record CreateBookingRequest(
    string TripId,
    IReadOnlyList<int> Seats,
    string PassengerName,
    PaymentMethodKind Method,
    string AccountNumber);

/// <summary>Reçu du paiement (simulé : toujours accepté).</summary>
public sealed record PaymentDto(
    PaymentMethodKind Method,
    string AccountMasked,
    int TicketsAmount,
    int ServiceFee,
    int Total,
    string TransactionId,
    DateTime PaidAt)
{
    [JsonIgnore]
    public string MethodLabel => Method switch
    {
        PaymentMethodKind.OrangeMoney => "Orange Money",
        PaymentMethodKind.MtnMoMo => "MTN Mobile Money",
        _ => "Carte bancaire",
    };
}

/// <summary>Un billet acheté. <see cref="QrPayload"/> est le contenu signé du QR code.</summary>
public sealed record BookingDto(
    string Reference,
    TripDto Trip,
    IReadOnlyList<int> Seats,
    string PassengerName,
    PaymentDto Payment,
    BookingStatus Status,
    DateTime? BoardedAt,
    string QrPayload,
    DateTime CreatedAt)
{
    /// <summary>Un billet expire à l'arrivée du bus.</summary>
    [JsonIgnore] public bool IsExpired => Trip.Arrival < DateTime.Now;
    [JsonIgnore] public bool IsBoarded => Status == BookingStatus.Boarded;
}

/// <summary><see cref="Payload"/> : contenu du QR code, ou référence saisie à la main (BT-XXXXXX).</summary>
public sealed record VerifyRequest(string Payload, bool Board);

public sealed record VerificationDto(
    VerificationResult Result,
    string Title,
    string Message,
    string? Reference,
    TripDto? Trip,
    IReadOnlyList<int> Seats,
    string? PassengerName,
    DateTime? BoardedAt)
{
    /// <summary>Le contrôleur peut faire embarquer le voyageur.</summary>
    [JsonIgnore] public bool CanBoard => Result is VerificationResult.Valid or VerificationResult.ValidOffline;
}

public sealed record ApiError(string Message);
