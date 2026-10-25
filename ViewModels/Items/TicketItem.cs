using BusTicket.Controls;
using BusTicket.Core;

namespace BusTicket.ViewModels.Items;

/// <summary>Un billet tel qu'affiché dans « Mes billets ».</summary>
public record TicketItem(BookingDto Booking, int Index)
{
    private TripDto Trip => Booking.Trip;

    public string Reference => Booking.Reference;
    public string From => Trip.From;
    public string To => Trip.To;
    public string DateText => Formatters.ShortDate(Trip.Departure);
    public string DepartureTime => Formatters.Time(Trip.Departure);
    public string ArrivalTime => Formatters.Time(Trip.Arrival);
    public bool ArrivesNextDay => Trip.ArrivesNextDay;
    public string Details => $"{Formatters.Seats(Booking.Seats)} · {Trip.Company}";
    public string PriceText => Formatters.Price(Booking.Payment.Total);

    public ChipKind Status => Booking.IsExpired ? ChipKind.Expired : Booking.IsBoarded ? ChipKind.Boarded : ChipKind.Valid;
    public bool IsActive => !Booking.IsExpired;

    /// <summary>« Départ dans 3 h », « Demain », « Dans 4 jours »… (billets à venir seulement).</summary>
    public string? Countdown
    {
        get
        {
            if (Booking.IsExpired || Booking.IsBoarded) return null;
            var left = Trip.Departure - DateTime.Now;
            if (left <= TimeSpan.Zero) return "En route";
            if (left.TotalHours < 1) return $"Départ dans {Math.Max(1, (int)left.TotalMinutes)} min";
            if (Trip.Departure.Date == DateTime.Today) return $"Départ dans {(int)left.TotalHours} h";
            var days = (Trip.Departure.Date - DateTime.Today).Days;
            return days == 1 ? "Demain" : $"Dans {days} jours";
        }
    }

    public bool HasCountdown => Countdown is not null;
    /// <summary>Départ aujourd'hui : le compte à rebours passe en orange.</summary>
    public bool IsSoon => HasCountdown && Trip.Departure.Date <= DateTime.Today;
    public int EntranceDelay => Math.Min(Index, 6) * 60;
}
