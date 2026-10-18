using BusTicket.Controls;
using BusTicket.Helpers;
using BusTicket.Models;

namespace BusTicket.ViewModels.Items;

/// <summary>Un billet tel qu'affiché dans « Mes billets ».</summary>
public record TicketItem(Booking Booking)
{
    public string DateText => $"{Formatters.ShortDate(Booking.Trip.Departure)} · {Formatters.Time(Booking.Trip.Departure)}";
    public string Route => Booking.Trip.Route;
    public string Details => $"{Formatters.Seats(Booking.Seats)} · {Booking.Trip.Company}";
    public ChipKind Status => Booking.IsExpired ? ChipKind.Expired : ChipKind.Valid;
}
