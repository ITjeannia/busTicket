using System.Text.RegularExpressions;
using BusTicket.Models;

namespace BusTicket.Services;

/// <summary>
/// Billets de l'utilisateur et paiement (simulé).
/// Les écrans s'abonnent à <see cref="Changed"/> pour se mettre à jour.
/// </summary>
public partial class BookingRepository
{
    private readonly List<Booking> _bookings = MockData.InitialBookings();

    public event EventHandler? Changed;

    public IReadOnlyList<Booking> Upcoming => _bookings.Where(b => !b.IsExpired).ToList();
    public IReadOnlyList<Booking> Past => _bookings.Where(b => b.IsExpired).ToList();

    /// <summary>Règle de démo : un numéro qui se termine par « 000 » est refusé.</summary>
    public async Task<Booking> PayAsync(Trip trip, IReadOnlyList<int> seats, string passengerName,
                                        PaymentMethod method, string accountNumber)
    {
        await Task.Delay(1500);
        if (NonDigits().Replace(accountNumber, "").EndsWith("000"))
            throw new AppException("Solde insuffisant ou paiement non validé");

        var booking = new Booking($"BT-{Random.Shared.Next(10000, 99999)}", trip, seats,
                                  passengerName, method, MockData.ServiceFee);
        _bookings.Insert(0, booking);
        Changed?.Invoke(this, EventArgs.Empty);
        return booking;
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigits();
}
