using BusTicket.Helpers;
using BusTicket.Models;

namespace BusTicket.ViewModels.Items;

/// <summary>Un départ tel qu'affiché dans la liste des résultats.</summary>
public record TripItem(Trip Trip)
{
    public string DepartureTime => Formatters.Time(Trip.Departure);
    public string ArrivalTime => Formatters.Time(Trip.Arrival);
    public string PriceText => Trip.IsFull ? "—" : Formatters.Price(Trip.Price);
    public string Details => $"{Trip.Route} · {Formatters.Duration(Trip.Duration)} · {Trip.StopsLabel}";
    public string Company => Trip.Company;
    public string SeatsText => $"{Trip.AvailableSeats} places";
    public bool IsFull => Trip.IsFull;
    public bool IsAvailable => !Trip.IsFull;
}
