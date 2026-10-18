namespace BusTicket.Models;

/// <summary>Une réservation payée : c'est ce qui est affiché comme billet.</summary>
public record Booking(
    string Reference,
    Trip Trip,
    IReadOnlyList<int> Seats,
    string PassengerName,
    PaymentMethod PaymentMethod,
    int ServiceFee)
{
    public int Total => Trip.Price * Seats.Count + ServiceFee;

    /// <summary>Un billet expire à l'arrivée du bus.</summary>
    public bool IsExpired => Trip.Arrival < DateTime.Now;
}
